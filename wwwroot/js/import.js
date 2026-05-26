// ============================================================
// GLOBAL STATE
// ============================================================
let currentMode = 1;
let confirmCallback = null;
const pathBase = window.pathBase || '';

// Mode 2 state
let mode2Rows = [];       // [{id, projectId, projectName, customer, phase, hours:{date:val}}]
let mode2RowCtr = 0;

// Mode 3 state
let bulkAllData = {};
let bulkTableRows = {};   // {empId: [{id, projectId, projectName, customer, phase, hours:{date:val}}]}
let bulkRowCtr = 0;
let bulkActiveEmpId = null;

// ============================================================
// SEARCHABLE SELECT COMPONENT (unchanged)
// ============================================================
function toggleSearchableSelect(sdId) {
    const sd = document.getElementById(sdId);
    const trigger = sd.querySelector('.searchable-select-trigger');
    const dropdown = sd.querySelector('.searchable-select-dropdown');
    const searchInput = sd.querySelector('.searchable-select-search');
    const isOpen = dropdown.classList.contains('open');
    document.querySelectorAll('.searchable-select-dropdown.open').forEach(d => {
        d.classList.remove('open');
        d.closest('.searchable-select').querySelector('.searchable-select-trigger').classList.remove('open');
    });
    if (!isOpen) {
        dropdown.classList.add('open'); trigger.classList.add('open');
        setTimeout(() => searchInput && searchInput.focus(), 50);
    }
}
function filterSearchableSelect(sdId, keyword) {
    const sd = document.getElementById(sdId);
    const items = sd.querySelectorAll('.searchable-select-item');
    const kw = keyword.toLowerCase().trim(); let cnt = 0;
    items.forEach(i => { const t=i.textContent.toLowerCase(); if(kw===''||t.includes(kw)){i.classList.remove('hidden-item');cnt++;}else i.classList.add('hidden-item'); });
    const em = sd.querySelector('.searchable-select-empty'); if(em) em.remove();
    if (cnt===0&&kw!=='') { const e=document.createElement('div'); e.className='searchable-select-empty'; e.textContent='Không tìm thấy nhân viên'; sd.querySelector('.searchable-select-options').appendChild(e); }
}
function selectSearchableItem(sdId, value, label) {
    const sd = document.getElementById(sdId);
    const targetSelectId = sd.getAttribute('data-target');
    const display = sd.querySelector('.searchable-select-display');
    const dropdown = sd.querySelector('.searchable-select-dropdown');
    const trigger = sd.querySelector('.searchable-select-trigger');
    const searchInput = sd.querySelector('.searchable-select-search');
    display.textContent = label; display.classList.add('selected');
    const hiddenSelect = document.getElementById(targetSelectId);
    hiddenSelect.value = value;
    for (let o of hiddenSelect.options) { if(o.value===String(value)){o.selected=true;break;} }
    sd.querySelectorAll('.searchable-select-item').forEach(i => i.classList.toggle('active', i.getAttribute('data-value')===String(value)));
    dropdown.classList.remove('open'); trigger.classList.remove('open');
    if (searchInput) { searchInput.value=''; filterSearchableSelect(sdId,''); }
}
function resetSearchableSelect(sdId, placeholder) {
    const sd = document.getElementById(sdId); if(!sd) return;
    sd.querySelector('.searchable-select-display').textContent = placeholder;
    sd.querySelector('.searchable-select-display').classList.remove('selected');
    sd.querySelector('.searchable-select-options').innerHTML = `<div class="searchable-select-placeholder">${placeholder}</div>`;
    const si = sd.querySelector('.searchable-select-search'); if(si) si.value='';
}
function populateSearchableSelect(sdId, employees, placeholder) {
    const sd = document.getElementById(sdId); if(!sd) return;
    const oc = sd.querySelector('.searchable-select-options');
    const d = sd.querySelector('.searchable-select-display');
    d.textContent = placeholder; d.classList.remove('selected'); oc.innerHTML='';
    employees.forEach(emp => {
        const fn=`${emp.first_name} ${emp.last_name}`.trim()||emp.nickname||emp.emp_code;
        const item=document.createElement('div'); item.className='searchable-select-item';
        item.setAttribute('data-value',emp.id); item.textContent=fn;
        item.onclick=()=>selectSearchableItem(sdId,emp.id,fn);
        oc.appendChild(item);
    });
}
document.addEventListener('click', function(e) {
    if (!e.target.closest('.searchable-select')) {
        document.querySelectorAll('.searchable-select-dropdown.open').forEach(d => {
            d.classList.remove('open');
            d.closest('.searchable-select').querySelector('.searchable-select-trigger').classList.remove('open');
        });
    }
});

// ============================================================
// MODE SWITCH / INIT
// ============================================================
function switchMode(mode) {
    currentMode = mode;
    document.querySelectorAll('.mode-btn').forEach(b=>b.classList.remove('active'));
    document.getElementById(`modeBtn${mode}`).classList.add('active');
    document.querySelectorAll('.mode-content').forEach(c=>c.classList.add('hidden'));
    document.getElementById(`mode${mode}`).classList.remove('hidden');
}
document.addEventListener('DOMContentLoaded', function() {
    generateWeeks(); generateHoursAndMinutes(); setupDepartmentChangeListeners(); initMode1ProjectRows();
    if (window.userDepartmentId) {
        const deptId = String(window.userDepartmentId);
        loadEmployees(deptId,'employee1'); loadEmployees(deptId,'employee2'); loadEmployeesAsCheckboxes(deptId);
    }
});

// ============================================================
// WEEK / DATE HELPERS
// ============================================================
const MONTH_NAMES = { vi:['Tháng 1','Tháng 2','Tháng 3','Tháng 4','Tháng 5','Tháng 6','Tháng 7','Tháng 8','Tháng 9','Tháng 10','Tháng 11','Tháng 12'], en:['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'], cn:['1月','2月','3月','4月','5月','6月','7月','8月','9月','10月','11月','12月'] };
const DAY_NAMES = { vi:['Thứ 2','Thứ 3','Thứ 4','Thứ 5','Thứ 6','Thứ 7','CN'], en:['Mon','Tue','Wed','Thu','Fri','Sat','Sun'], cn:['一','二','三','四','五','六','日'] };
function getDayName(idx) { const lang=localStorage.getItem('heatmap_lang')||'vi'; return (DAY_NAMES[lang]||DAY_NAMES['vi'])[idx]; }
function formatDayLabel(i, dateObj) {
    const lang=localStorage.getItem('heatmap_lang')||'vi';
    const dayName=getDayName(i);
    const d=dateObj.getDate(),m=dateObj.getMonth(),y=dateObj.getFullYear();
    const dd=String(d).padStart(2,'0'),mm=String(m+1).padStart(2,'0');
    const mon=MONTH_NAMES[lang]?.[m]||MONTH_NAMES['en'][m];
    let dateStr;
    if(lang==='en') dateStr=`${y} ${dd} ${mon}`;
    else if(lang==='cn') dateStr=`${y}年${mon}${dd}日`;
    else dateStr=`${dd}/${mm}/${y}`;
    return { dayName, dateStr, label:`${dayName} - ${dateStr}` };
}
function getWeekLabel(w) { const lang=localStorage.getItem('heatmap_lang')||'vi'; if(lang==='en') return `Week ${w}`; if(lang==='cn') return `第 ${w} 周`; return `Tuần ${w}`; }
function formatDateLocale(date) {
    const lang=localStorage.getItem('heatmap_lang')||'vi';
    const d=date.getDate(),m=date.getMonth(),y=date.getFullYear();
    const dd=String(d).padStart(2,'0'),mm=String(m+1).padStart(2,'0');
    const mon=MONTH_NAMES[lang]?.[m]||MONTH_NAMES['en'][m];
    if(lang==='en') return `${y} ${dd} ${mon}`; if(lang==='cn') return `${y}年${mon}${dd}日`; return `${dd}/${mm}/${y}`;
}
function generateWeeks() {
    ['week1','week2','week3'].forEach(id => {
        const s=document.getElementById(id); if(!s) return;
        const prev=s.value; const cur=new Date().getFullYear(); const cw=getWeekNumber(new Date());
        while(s.options.length>1) s.remove(1);
        for(let w=1;w<=52;w++) {
            const jan1=new Date(cur,0,1); const wd=new Date(jan1.setDate(jan1.getDate()+(w-1)*7));
            const mon=getMonday(wd); const sun=new Date(mon); sun.setDate(mon.getDate()+6);
            const o=document.createElement('option');
            o.value=`${w}|${formatDate(mon)}|${formatDate(sun)}`;
            o.textContent=`${getWeekLabel(w)} (${formatDateLocale(mon)} - ${formatDateLocale(sun)})`;
            if(w===cw) o.selected=true; s.appendChild(o);
        }
        if(prev) { const pn=prev.split('|')[0]; for(let o of s.options) { if(o.value.startsWith(pn+'|')){ s.value=o.value; break; } } }
    });
}
document.addEventListener('i18n:applied', function() { generateWeeks(); });
function generateHoursAndMinutes() {
    const hs=document.getElementById('hour1'), ms=document.getElementById('minute1');
    if(hs) for(let h=0;h<=23;h++){const o=document.createElement('option');o.value=h;o.textContent=String(h).padStart(2,'0');hs.appendChild(o);}
    if(ms) for(let m=0;m<=59;m++){const o=document.createElement('option');o.value=m;o.textContent=String(m).padStart(2,'0');ms.appendChild(o);}
    if(hs&&ms) {
        const calc=()=>{if(hs.value!==''&&ms.value!==''){const dec=parseInt(hs.value)+(parseInt(ms.value)/60);if(document.getElementById('hourDecimal1'))document.getElementById('hourDecimal1').value=dec.toFixed(2);if(document.getElementById('hourDisplay1'))document.getElementById('hourDisplay1').textContent=`= ${dec.toFixed(2)} giờ`;}};
        hs.addEventListener('change',calc); ms.addEventListener('change',calc);
    }
}
function setupDepartmentChangeListeners() {
    document.getElementById('department1')?.addEventListener('change',function(){loadEmployees(this.value,'employee1');});
    document.getElementById('department2')?.addEventListener('change',function(){loadEmployees(this.value,'employee2');});
    document.getElementById('department3')?.addEventListener('change',function(){loadEmployeesAsCheckboxes(this.value);});
    const w2=document.getElementById('week2');
    if(w2){w2.addEventListener('change',function(){generateDayCheckboxes(this.value,'dayCheckboxes2');}); if(w2.value) w2.dispatchEvent(new Event('change'));}
    const w3=document.getElementById('week3');
    if(w3){w3.addEventListener('change',function(){generateDayCheckboxes(this.value,'dayCheckboxes3');}); if(w3.value) w3.dispatchEvent(new Event('change'));}
}
function loadEmployees(departmentId, targetSelectId) {
    const es=document.getElementById(targetSelectId); const sdId=targetSelectId+'-sd'; const sd=document.getElementById(sdId);
    es.innerHTML='<option value="">Đang tải...</option>';
    if(sd){const d=sd.querySelector('.searchable-select-display');const oc=sd.querySelector('.searchable-select-options');if(d){d.textContent='Đang tải...';d.classList.remove('selected');}if(oc)oc.innerHTML='<div class="searchable-select-placeholder">Đang tải...</div>';}
    if(!departmentId){es.innerHTML='<option value="">-- Chọn bộ phận trước --</option>';if(sd)resetSearchableSelect(sdId,'-- Chọn bộ phận trước --');return;}
    fetch(`${pathBase}/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
    .then(r=>r.json()).then(emps=>{
        es.innerHTML='<option value="">-- Chọn nhân viên --</option>';
        emps.forEach(e=>{const fn=`${e.first_name} ${e.last_name}`.trim()||e.nickname||e.emp_code;const o=document.createElement('option');o.value=e.id;o.textContent=fn;es.appendChild(o);});
        if(sd) populateSearchableSelect(sdId,emps,'-- Chọn nhân viên --');
    }).catch(()=>{es.innerHTML='<option value="">Lỗi khi tải danh sách</option>';if(sd)resetSearchableSelect(sdId,'Lỗi khi tải danh sách');});
}
function loadEmployeesAsCheckboxes(departmentId) {
    const c=document.getElementById('employeeCheckboxes3'); c.innerHTML='<p class="text-gray-400 text-center py-4">Đang tải...</p>';
    if(!departmentId){c.innerHTML='<p class="text-gray-400 text-center py-4">Vui lòng chọn bộ phận</p>';return;}
    const sb=document.getElementById('searchEmployee3'); if(sb) sb.value='';
    fetch(`${pathBase}/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
    .then(r=>r.json()).then(emps=>{
        c.innerHTML=''; if(!emps.length){c.innerHTML='<p class="text-gray-400 text-center py-4">Không có nhân viên</p>';return;}
        emps.forEach(e=>{const fn=`${e.first_name} ${e.last_name}`.trim()||e.nickname||e.emp_code;const d=document.createElement('div');d.className='employee-checkbox';d.innerHTML=`<input type="checkbox" id="emp${e.id}" value="${e.id}" data-name="${fn}" onchange="handleEmployeeCheckboxChange(this)"><label for="emp${e.id}" class="cursor-pointer select-none">${fn}</label>`;c.appendChild(d);});
    }).catch(()=>{c.innerHTML='<p class="text-red-400 text-center py-4">Lỗi khi tải danh sách</p>';});
}
function handleEmployeeCheckboxChange(cb) { cb.closest('.employee-checkbox').classList.toggle('selected',cb.checked); }
function filterEmployeeCheckboxes(searchText) {
    const kw=searchText.toLowerCase().trim();
    document.querySelectorAll('#employeeCheckboxes3 .employee-checkbox').forEach(b=>{const n=b.querySelector('label')?.textContent.toLowerCase()??'';b.style.display=(!kw||n.includes(kw))?'':'none';});
}
function generateDayCheckboxes(weekValue, containerId) {
    const c=document.getElementById(containerId); c.innerHTML='';
    if(!weekValue){const lang=localStorage.getItem('heatmap_lang')||'vi';const msg=lang==='en'?'Please select a week':lang==='cn'?'请先选择周':'Vui lòng chọn tuần';c.innerHTML=`<p class="text-gray-400 text-center py-4">${msg}</p>`;return;}
    const [,startDateStr]=weekValue.split('|'); const startDate=new Date(startDateStr.split('/').reverse().join('-'));
    for(let i=0;i<7;i++){
        const cur=new Date(startDate); cur.setDate(startDate.getDate()+i);
        const {dayName,dateStr,label}=formatDayLabel(i,cur); const vs=formatDate(cur);
        const div=document.createElement('div'); div.className='day-checkbox';
        div.innerHTML=`<input type="checkbox" id="day${containerId}_${i}" value="${vs}" data-day="${dayName}" data-label="${label}" onchange="handleDayCheckboxChange(this,'${containerId}')"><label for="day${containerId}_${i}" class="cursor-pointer select-none flex-1">${label}</label>`;
        c.appendChild(div);
    }
}
function handleDayCheckboxChange(checkbox, containerId) {
    checkbox.closest('.day-checkbox').classList.toggle('selected',checkbox.checked);
    if (containerId==='dayCheckboxes2') updateDayHoursList();
}
document.addEventListener('i18n:applied', function() {
    ['week2','week3'].forEach(wid => {
        const ws = document.getElementById(wid);
        if (!ws?.value) return;
        const cid = wid==='week2' ? 'dayCheckboxes2' : 'dayCheckboxes3';
        
        // ── Lưu các ngày đang tick trước khi rebuild ──────────────────
        const checkedDates = new Set(
            Array.from(document.querySelectorAll(`#${cid} input[type="checkbox"]:checked`))
            .map(cb => cb.value)
        );
        
        // ── Rebuild checkbox với ngôn ngữ mới ─────────────────────────
        generateDayCheckboxes(ws.value, cid);
        
        // ── Restore trạng thái tick ───────────────────────────────────
        if (checkedDates.size > 0) {
            document.querySelectorAll(`#${cid} input[type="checkbox"]`).forEach(cb => {
                if (checkedDates.has(cb.value)) {
                    cb.checked = true;
                    cb.closest('.day-checkbox')?.classList.add('selected');
                }
            });
        }
    });
});
function filterProjectsByCustomer(customerName, projectSelectId) {
    const s=document.getElementById(projectSelectId); if(!s) return;
    if(!customerName){s.innerHTML='<option value="">-- Chọn customer trước --</option>';s.disabled=true;s.classList.add('select-disabled');return;}
    s.disabled=false; s.classList.remove('select-disabled');
    const cv=s.value; s.innerHTML='<option value="">-- Chọn dự án --</option>';
    window.projectsData.filter(p=>p.NameCustomer===customerName).forEach(p=>{const o=document.createElement('option');o.value=p.IdProject;o.textContent=p.NameProject;if(p.IdProject==cv)o.selected=true;s.appendChild(o);});
}

// ============================================================
// MODE 1 - UNCHANGED (multi-select per block)
// ============================================================
function addMode1ProjectRow(savedData) { addProjectRow(document.getElementById('projectRows1'), savedData||null); }
function initMode1ProjectRows() { const c=document.getElementById('projectRows1'); if(!c) return; c.innerHTML=''; initProjectBlock(c,null); }
function initProjectBlock(rowsContainer, savedRows) {
    rowsContainer._multiSelectOpen=false;
    const msWrap=document.createElement('div'); msWrap.className='ms-wrap'; msWrap.style.cssText='position:relative;margin-bottom:0;';
    const msTrigger=document.createElement('div'); msTrigger.className='bulk-select ms-trigger';
    msTrigger.style.cssText='display:flex;align-items:center;justify-content:space-between;cursor:pointer;user-select:none;background:white;';
    msTrigger.innerHTML=`<span class="ms-trigger-label" style="color:#6b7280;">${t('import.col.project')}</span><svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" style="flex-shrink:0;color:#9ca3af;transition:transform 0.2s;"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/></svg>`;
    const msDropdown=document.createElement('div'); msDropdown.className='ms-dropdown';
    msDropdown.style.cssText=`display:none;position:fixed;z-index:9999;background:white;border:2px solid #dc2626;border-top:none;border-bottom-left-radius:1rem;border-bottom-right-radius:1rem;box-shadow:0 10px 40px rgba(0,0,0,0.12);max-height:220px;overflow-y:auto;`;
    document.body.appendChild(msDropdown); msDropdown._triggerEl=msTrigger;
    const msSearch=document.createElement('input'); msSearch.type='text'; msSearch.placeholder=t('import.search_proj');
    msSearch.style.cssText='width:100%;padding:8px 12px;border:none;border-bottom:1px solid #f3f4f6;font-size:0.85rem;outline:none;box-sizing:border-box;';
    msSearch.oninput=function(){const kw=this.value.toLowerCase();msDropdown.querySelectorAll('.ms-item').forEach(item=>item.style.display=item.dataset.name.toLowerCase().includes(kw)?'':'none');};
    msDropdown.appendChild(msSearch);
    window.projectsData.forEach(p=>{
        const item=document.createElement('label'); item.className='ms-item'; item.dataset.name=p.NameProject;
        item.style.cssText='display:flex;align-items:center;gap:10px;padding:9px 14px;cursor:pointer;font-size:0.875rem;color:#374151;transition:background 0.1s;';
        item.onmouseenter=()=>item.style.background='#fef2f2'; item.onmouseleave=()=>{if(!item.querySelector('input').checked)item.style.background='';else item.style.background='#fff5f5';};
        const cb=document.createElement('input'); cb.type='checkbox'; cb.value=p.IdProject; cb.dataset.customer=p.NameCustomer||''; cb.dataset.projectName=p.NameProject;
        cb.style.cssText='accent-color:#dc2626;width:16px;height:16px;flex-shrink:0;cursor:pointer;';
        cb.onchange=function(){
            if(this.checked){item.style.background='#fff5f5';addProjectRow(rowsContainer,{project:p.IdProject,customer:p.NameCustomer,projectName:p.NameProject});}
            else{item.style.background='';const tr=rowsContainer.querySelector(`.bulk-project-row[data-project-id="${p.IdProject}"]`);if(tr)tr.remove();}
            updateMsTriggerLabel(msTrigger,msDropdown);
        };
        item.appendChild(cb); item.appendChild(document.createTextNode(p.NameProject)); msDropdown.appendChild(item);
    });
    msTrigger.onclick=function(){
        const isOpen=msDropdown.style.display==='block';
        if(isOpen){msDropdown.style.display='none';msTrigger.style.borderBottomLeftRadius='';msTrigger.style.borderBottomRightRadius='';msTrigger.querySelector('svg').style.transform='';}
        else{
            document.querySelectorAll('.ms-dropdown').forEach(d=>{if(d!==msDropdown){d.style.display='none';const t=d._triggerEl;if(t){t.style.borderBottomLeftRadius='';t.style.borderBottomRightRadius='';t.querySelector('svg').style.transform='';}}} );
            const r=msTrigger.getBoundingClientRect();
            msDropdown.style.top=r.bottom+'px';msDropdown.style.left=r.left+'px';msDropdown.style.width=r.width+'px';msDropdown.style.display='block';
            msTrigger.style.borderBottomLeftRadius='0';msTrigger.style.borderBottomRightRadius='0';msTrigger.querySelector('svg').style.transform='rotate(180deg)';
            setTimeout(()=>msSearch.focus(),50);
        }
    };
    msWrap.appendChild(msTrigger);
    rowsContainer.parentElement.insertBefore(msWrap,rowsContainer);
    rowsContainer._msWrap=msWrap; rowsContainer._msDropdown=msDropdown;
    if(savedRows&&savedRows.length>0){
        savedRows.forEach(r=>{addProjectRow(rowsContainer,r);if(r.project){const cb=msDropdown.querySelector(`input[value="${r.project}"]`);if(cb){cb.checked=true;cb.closest('.ms-item').style.background='#fff5f5';}}});
        updateMsTriggerLabel(msTrigger,msDropdown);
    }
}
function updateMsTriggerLabel(msTrigger,msDropdown){
    const checked=msDropdown.querySelectorAll('input:checked'); const label=msTrigger.querySelector('.ms-trigger-label');
    if(checked.length===0){label.textContent=t('import.col.project');label.style.color='#6b7280';}
    else{label.textContent=Array.from(checked).map(cb=>cb.dataset.projectName).join(', ');label.style.color='#111827';}
}
document.addEventListener('click',function(e){
    if(!e.target.closest('.ms-wrap')&&!e.target.closest('.ms-dropdown')){
        document.querySelectorAll('.ms-dropdown').forEach(d=>{if(d.style.display==='block'){d.style.display='none';const t=d._triggerEl;if(t){t.style.borderBottomLeftRadius='';t.style.borderBottomRightRadius='';t.querySelector('svg').style.transform='';}}});
    }
});
function addProjectRow(rowsContainer, savedData) {
    const row=document.createElement('div'); row.className='bulk-project-row'; if(savedData?.project)row.dataset.projectId=savedData.project;
    const pl=document.createElement('div'); pl.className='bulk-select bulk-project-label';
    pl.style.cssText='display:flex;align-items:center;padding:0 12px;background:#f9fafb;border-radius:0.75rem;border:2px solid #e5e7eb;font-size:0.875rem;color:#111827;min-height:44px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;';
    const hp=document.createElement('input'); hp.type='hidden'; hp.className='bulk-project'; hp.value=savedData?.project||'';
    if(savedData?.project){const f=window.projectsData.find(p=>String(p.IdProject)===String(savedData.project));pl.textContent=f?f.NameProject:savedData.projectName||'';}
    else{pl.textContent='-- Project --';pl.style.color='#9ca3af';} pl.appendChild(hp);
    const cs=document.createElement('select'); cs.className='bulk-select bulk-customer select-disabled'; cs.disabled=true;
    cs.style.cssText='background:#f3f4f6;color:#6b7280;cursor:not-allowed;opacity:0.85;';
    const cns=Array.from(new Set(window.projectsData.map(p=>p.NameCustomer).filter(Boolean)));
    cs.innerHTML=`<option value="">-- Customer --</option>`+cns.map(n=>`<option value="${n}">${n}</option>`).join('');
    const cn=savedData?.customer||(savedData?.project?window.projectsData.find(p=>String(p.IdProject)===String(savedData.project))?.NameCustomer:'');
    if(cn) cs.value=cn;
    const ppo=(window.projectPhasesData||[]).map(pp=>`<option value="${pp}"${savedData?.projectPhase===pp?' selected':''}>${pp}</option>`).join('');
    const pps=document.createElement('select'); pps.className='bulk-select bulk-pp'; pps.innerHTML=`<option value="">-- Proj. Phase --</option>${ppo}`;
    const hi=document.createElement('input'); hi.type='number'; hi.className='bulk-hours-input'; hi.placeholder='VD: 4.5'; hi.min='0.5'; hi.max='24'; hi.step='0.5'; if(savedData?.hours)hi.value=savedData.hours;
    const db=document.createElement('button'); db.type='button'; db.className='bulk-delete-btn'; db.innerHTML='✕';
    db.onclick=function(){
        const pid=row.dataset.projectId; const msd=rowsContainer._msDropdown;
        if(pid&&msd){const cb=msd.querySelector(`input[value="${pid}"]`);if(cb){cb.checked=false;cb.closest('.ms-item').style.background='';updateMsTriggerLabel(rowsContainer._msWrap.querySelector('.ms-trigger'),msd);}}
        row.remove();
    };
    row.appendChild(pl); row.appendChild(cs); row.appendChild(pps); row.appendChild(hi); row.appendChild(db);
    rowsContainer.appendChild(row);
}
document.addEventListener('i18n:applied',function(){
    document.querySelectorAll('.ms-dropdown input[type="text"]').forEach(i=>i.placeholder=t('import.search_proj'));
    document.querySelectorAll('.ms-trigger').forEach(t=>{const l=t.querySelector('.ms-trigger-label');if(!l)return;if(l.style.color==='rgb(107, 114, 128)'||l.style.color==='#6b7280')l.textContent=t('import.col.project');});
});

// ============================================================
// SHARED: BUILD PROJECT TABLE with tick-dropdown
// ============================================================

/**
* Tạo UI = multi-select dropdown (tick để thêm hàng) + bảng với cột ngày.
* @param {string}   wrapperId  - id của div chứa kết quả
* @param {Array}    dates      - [{date:'dd/mm/yyyy', label:'...'}]
* @param {Array}    rows       - state rows [{id,projectId,projectName,customer,phase,hours:{}}]
* @param {Object}   opts       - { onAddRow, onRemoveRow, dropdownId, tbodyId }
*/
function buildTickTableUI(wrapperId, dates, rows, opts) {
    const wrapper = document.getElementById(wrapperId);
    if (!wrapper) return;
    wrapper.innerHTML = '';
    
    // ── 1. Multi-select dropdown (tick project → thêm hàng) ──────────
    const msWrap = document.createElement('div');
    msWrap.className = 'ms-wrap';
    msWrap.style.cssText = 'position:relative; margin-bottom:8px;';
    
    const msTrigger = document.createElement('div');
    msTrigger.className = 'bulk-select ms-trigger';
    msTrigger.style.cssText = 'display:flex; align-items:center; justify-content:space-between; cursor:pointer; user-select:none; background:white;';
    msTrigger.innerHTML = `<span class="ms-trigger-label" style="color:#6b7280;">${t('import.col.project') || 'Chọn dự án'}</span>
    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" style="flex-shrink:0;color:#9ca3af;transition:transform 0.2s;">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/>
    </svg>`;
    
    const msDropdown = document.createElement('div');
    // ms-dropdown class bắt buộc để global close handler nhận ra
    msDropdown.className = 'ms-dropdown';
    msDropdown.style.cssText = 'display:none; position:fixed; z-index:9999; background:white; border:2px solid #dc2626; border-top:none; border-bottom-left-radius:1rem; border-bottom-right-radius:1rem; box-shadow:0 10px 40px rgba(0,0,0,0.12); max-height:220px; overflow-y:auto;';
    msDropdown._triggerEl = msTrigger;
    if (opts.dropdownId) msDropdown.id = opts.dropdownId;
    document.body.appendChild(msDropdown);
    
    // Search box
    const search = document.createElement('input');
    search.type = 'text'; search.placeholder = t('import.search_proj') || 'Tìm dự án...';
    search.style.cssText = 'width:100%; padding:8px 12px; border:none; border-bottom:1px solid #f3f4f6; font-size:0.85rem; outline:none; box-sizing:border-box;';
    search.oninput = () => { const kw=search.value.toLowerCase(); msDropdown.querySelectorAll('.ms-item').forEach(i=>i.style.display=i.dataset.name.toLowerCase().includes(kw)?'':'none'); };
    msDropdown.appendChild(search);
    
    // Project items
    window.projectsData.forEach(p => {
        const item = document.createElement('label');
        item.className = 'ms-item'; item.dataset.name = p.NameProject;
        item.style.cssText = 'display:flex; align-items:center; gap:10px; padding:9px 14px; cursor:pointer; font-size:0.875rem; color:#374151; transition:background 0.1s;';
        item.onmouseenter = () => item.style.background = '#fef2f2';
        item.onmouseleave = () => { if (!item.querySelector('input').checked) item.style.background=''; else item.style.background='#fff5f5'; };
        
        const cb = document.createElement('input'); cb.type='checkbox'; cb.value=p.IdProject;
        cb.style.cssText = 'accent-color:#dc2626; width:16px; height:16px; flex-shrink:0; cursor:pointer;';
        
        // Restore checked state from current rows
        const alreadyInRows = rows.some(r => String(r.projectId) === String(p.IdProject));
        if (alreadyInRows) { cb.checked=true; item.style.background='#fff5f5'; }
        
        cb.onchange = function() {
            if (this.checked) {
                item.style.background = '#fff5f5';
                opts.onAddRow({ projectId: p.IdProject, projectName: p.NameProject, customer: p.NameCustomer || '' });
            } else {
                item.style.background = '';
                // Remove last row with this projectId
                opts.onRemoveRow(p.IdProject, 'byProjectId');
            }
            // Update trigger label
            const allChecked = msDropdown.querySelectorAll('input:checked');
            const lbl = msTrigger.querySelector('.ms-trigger-label');
            if (allChecked.length === 0) { lbl.textContent = t('import.col.project')||'Chọn dự án'; lbl.style.color='#6b7280'; }
            else { lbl.textContent = Array.from(allChecked).map(c=>c.closest('.ms-item').dataset.name).join(', '); lbl.style.color='#111827'; }
        };
        
        item.appendChild(cb); item.appendChild(document.createTextNode(p.NameProject));
        msDropdown.appendChild(item);
    });
    
    // Toggle dropdown open/close
    msTrigger.onclick = function() {
        const isOpen = msDropdown.style.display === 'block';
        if (isOpen) {
            msDropdown.style.display='none';
            msTrigger.style.borderBottomLeftRadius=''; msTrigger.style.borderBottomRightRadius='';
            msTrigger.style.borderTopLeftRadius=''; msTrigger.style.borderTopRightRadius='';
            msTrigger.querySelector('svg').style.transform='';
        } else {
            // Đóng tất cả dropdown khác
            document.querySelectorAll('.ms-dropdown').forEach(d => {
                if (d !== msDropdown && d.style.display==='block') {
                    d.style.display='none';
                    const t = d._triggerEl;
                    if (t) { t.style.borderBottomLeftRadius=''; t.style.borderBottomRightRadius=''; t.style.borderTopLeftRadius=''; t.style.borderTopRightRadius=''; t.querySelector('svg').style.transform=''; }
                }
            });
            const r = msTrigger.getBoundingClientRect();
            const dropH = 240; // max-height dropdown
            const spaceBelow = window.innerHeight - r.bottom;
            const spaceAbove = r.top;
            
            if (spaceBelow >= dropH || spaceBelow >= spaceAbove) {
                // Mở xuống dưới
                msDropdown.style.top = r.bottom + 'px';
                msDropdown.style.bottom = '';
                msDropdown.style.borderRadius = '0 0 1rem 1rem';
                msDropdown.style.borderTop = 'none';
                msDropdown.style.borderBottom = '2px solid #dc2626';
                msTrigger.style.borderBottomLeftRadius='0'; msTrigger.style.borderBottomRightRadius='0';
                msTrigger.style.borderTopLeftRadius=''; msTrigger.style.borderTopRightRadius='';
            } else {
                // Mở lên trên (khi trigger gần cuối màn hình)
                msDropdown.style.bottom = (window.innerHeight - r.top) + 'px';
                msDropdown.style.top = '';
                msDropdown.style.borderRadius = '1rem 1rem 0 0';
                msDropdown.style.borderBottom = 'none';
                msDropdown.style.borderTop = '2px solid #dc2626';
                msTrigger.style.borderTopLeftRadius='0'; msTrigger.style.borderTopRightRadius='0';
                msTrigger.style.borderBottomLeftRadius=''; msTrigger.style.borderBottomRightRadius='';
            }
            msDropdown.style.left = r.left + 'px';
            msDropdown.style.width = r.width + 'px';
            msDropdown.style.display = 'block';
            msTrigger.querySelector('svg').style.transform = 'rotate(180deg)';
            setTimeout(() => search.focus(), 50);
        }
    };
    
    // Restore trigger label
    const checkedRows = rows.filter(r => window.projectsData.some(p=>String(p.IdProject)===String(r.projectId)));
    if (checkedRows.length > 0) {
        const lbl = msTrigger.querySelector('.ms-trigger-label');
        const uniqueNames = [...new Set(checkedRows.map(r=>r.projectName))];
        lbl.textContent = uniqueNames.join(', '); lbl.style.color='#111827';
    }
    
    msWrap.appendChild(msTrigger);
    wrapper.appendChild(msWrap);
    
    // ── 2. Table ───────────────────────────────────────────────────────
    const tableWrap = document.createElement('div');
    tableWrap.style.cssText = 'overflow-x:auto; border:2px solid #e5e7eb; border-radius:1rem;';
    
    if (rows.length === 0) {
        tableWrap.innerHTML = `<p style="padding:16px 20px; color:#9ca3af; font-size:0.875rem; text-align:center;">${t('import.hint.select_proj') || '← Chọn dự án từ dropdown để thêm hàng'}</p>`;
        wrapper.appendChild(tableWrap);
        return;
    }
    
    const table = document.createElement('table');
    table.style.cssText = 'width:100%; border-collapse:collapse; font-size:0.875rem;';
    
    // Header
    const thead = document.createElement('thead');
    const htr = document.createElement('tr');
    htr.style.cssText = 'background:#f9fafb; border-bottom:2px solid #e5e7eb;';
    const thS = 'padding:9px 10px; font-size:0.68rem; font-weight:800; text-transform:uppercase; letter-spacing:0.06em; color:#9ca3af; white-space:nowrap; text-align:left;';
    const thC = thS + 'text-align:center; min-width:70px;';
    htr.innerHTML =
    `<th style="${thS} min-width:150px;">${t('import.col.project')||'PROJECT'}</th>` +
    `<th style="${thS} min-width:110px;">${t('import.col.customer')||'CUSTOMER'}</th>` +
    `<th style="${thS} min-width:100px;">${t('import.col.projphase')||'PHASE'}</th>` +
    dates.map(d => {
        const parts = d.date.split('/');
        const dow = d.label.split(' - ')[0];
        const short = `${parseInt(parts[0])}-${MONTH_NAMES['en'][parseInt(parts[1])-1]}`;
        return `<th style="${thC}"><div>${dow}</div><div style="color:#dc2626;font-weight:900;font-size:0.75rem;">${short}</div></th>`;
    }).join('') +
    `<th style="width:36px;"></th>`;
    thead.appendChild(htr); table.appendChild(thead);
    
    // Body
    const tbody = document.createElement('tbody');
    if (opts.tbodyId) tbody.id = opts.tbodyId;
    
    rows.forEach(row => {
        const tr = document.createElement('tr');
        tr.dataset.rowId = row.id;
        tr.style.cssText = 'border-bottom:1px solid #f3f4f6; transition:background 0.1s;';
        tr.onmouseenter = () => tr.style.background = '#fafafa';
        tr.onmouseleave = () => tr.style.background = '';
        
        const td = (style) => { const el=document.createElement('td'); el.style=style||'padding:7px 10px; vertical-align:middle;'; return el; };
        
        // Project name (readonly)
        const tdP = td(); tdP.style.cssText = 'padding:7px 10px; vertical-align:middle;';
        tdP.innerHTML = `<div style="padding:6px 10px; background:#f9fafb; border:2px solid #e5e7eb; border-radius:0.6rem; font-size:0.875rem; color:#111827; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; max-width:160px;" title="${row.projectName}">${row.projectName||'--'}</div>`;
        tr.appendChild(tdP);
        
        // Customer (readonly)
        const tdC = td(); tdC.style.cssText = 'padding:7px 10px; vertical-align:middle;';
        tdC.innerHTML = `<div style="padding:6px 10px; background:#f9fafb; border:2px solid #e5e7eb; border-radius:0.6rem; font-size:0.875rem; color:#6b7280; white-space:nowrap;">${row.customer||'--'}</div>`;
        tr.appendChild(tdC);
        
        // Phase select
        const tdPh = td();
        const phSel = document.createElement('select'); phSel.className='bulk-select row-phase'; phSel.style.cssText='min-width:90px;width:100%;';
        phSel.innerHTML = `<option value="">-- Phase --</option>` + (window.projectPhasesData||[]).map(pp=>`<option value="${pp}"${pp===row.phase?' selected':''}>${pp}</option>`).join('');
        tdPh.appendChild(phSel); tr.appendChild(tdPh);
        
        // Hours per date
        dates.forEach(d => {
            const tdH = td('padding:7px 5px; text-align:center; vertical-align:middle;');
            const inp = document.createElement('input');
            inp.type='number'; inp.className='bulk-hours-input hours-cell';
            inp.dataset.date=d.date; inp.placeholder='-'; inp.min='0'; inp.max='24'; inp.step='0.5';
            inp.value = row.hours[d.date] || '';
            inp.style.cssText = 'width:62px; text-align:center; padding:0.4rem 0.3rem;';
            tdH.appendChild(inp); tr.appendChild(tdH);
        });
        
        // Delete
        const tdDel = td('padding:7px 5px; text-align:center; vertical-align:middle;');
        const delBtn = document.createElement('button'); delBtn.type='button'; delBtn.className='bulk-delete-btn'; delBtn.innerHTML='✕';
        delBtn.onclick = () => opts.onRemoveRow(row.id, 'byId');
        tdDel.appendChild(delBtn); tr.appendChild(tdDel);
        
        tbody.appendChild(tr);
    });
    table.appendChild(tbody);
    tableWrap.appendChild(table);
    wrapper.appendChild(tableWrap);
}

// ============================================================
// MODE 2 - TICK TABLE
// ============================================================
function getMode2Dates() {
    return Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'))
    .map(cb => ({ date: cb.value, label: cb.dataset.label }));
}

function updateDayHoursList() {
    saveMode2Rows();
    renderMode2Section();
}

function saveMode2Rows() {
    const tbody = document.getElementById('mode2-tbody');
    if (!tbody) return;
    tbody.querySelectorAll('tr[data-row-id]').forEach(tr => {
        const row = mode2Rows.find(r => r.id === parseInt(tr.dataset.rowId));
        if (!row) return;
        row.phase = tr.querySelector('.row-phase')?.value || '';
        tr.querySelectorAll('.hours-cell').forEach(inp => { row.hours[inp.dataset.date] = inp.value; });
    });
}

function renderMode2Section() {
    const section = document.getElementById('dayHoursSection2');
    const dates = getMode2Dates();
    if (dates.length === 0) { section.style.display='none'; return; }
    section.style.display = 'block';
    
    // Ensure all rows have all date keys
    mode2Rows.forEach(row => { dates.forEach(d => { if(!(d.date in row.hours)) row.hours[d.date]=''; }); });
    
    buildTickTableUI('dayHoursList2', dates, mode2Rows, {
        dropdownId: 'mode2-ms-dd',
        tbodyId: 'mode2-tbody',
        onAddRow: (projData) => {
            saveMode2Rows();
            mode2Rows.push({ id:mode2RowCtr++, projectId:projData.projectId, projectName:projData.projectName, customer:projData.customer, phase:'', hours:Object.fromEntries(dates.map(d=>[d.date,''])) });
            renderMode2Section();
        },
        onRemoveRow: (idOrProjectId, type) => {
            saveMode2Rows();
            if (type === 'byProjectId') {
                // Remove last row for that projectId, uncheck dd
                const lastIdx = [...mode2Rows].reverse().findIndex(r => String(r.projectId)===String(idOrProjectId));
                if (lastIdx >= 0) mode2Rows.splice(mode2Rows.length-1-lastIdx, 1);
            } else {
                // Remove by row id, uncheck if no more rows for that project
                const row = mode2Rows.find(r=>r.id===idOrProjectId);
                mode2Rows = mode2Rows.filter(r=>r.id!==idOrProjectId);
                if (row) {
                    const still = mode2Rows.some(r=>String(r.projectId)===String(row.projectId));
                    if (!still) {
                        const dd = document.getElementById('mode2-ms-dd');
                        if (dd) { const cb=dd.querySelector(`input[value="${row.projectId}"]`); if(cb){cb.checked=false;cb.closest('.ms-item').style.background='';} }
                    }
                }
            }
            renderMode2Section();
        }
    });
}

function copyFirstDayDataToAll() {
    saveMode2Rows();
    const dates = getMode2Dates();
    if (dates.length <= 1) { showErrorModal('Cần chọn ít nhất 2 ngày để copy'); return; }
    const firstDate = dates[0].date;
    const hasData = mode2Rows.some(r=>parseFloat(r.hours[firstDate])>0);
    if (!hasData) { showErrorModal('Vui lòng nhập giờ cho cột ngày đầu tiên trước'); return; }
    mode2Rows.forEach(row => { dates.slice(1).forEach(d => { row.hours[d.date]=row.hours[firstDate]; }); });
    renderMode2Section();
    showSuccessModal(`✓ Đã copy giờ từ ngày đầu cho ${dates.length-1} ngày còn lại!`);
}

// ============================================================
// MODE 3 - TICK TABLE PER EMPLOYEE
// ============================================================
function showBulkInputPopup() {
    const selEmps = Array.from(document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]:checked'));
    if (!selEmps.length) { showErrorModal('Vui lòng chọn ít nhất 1 nhân viên'); return; }
    const selDays = Array.from(document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]:checked'));
    if (!selDays.length) { showErrorModal('Vui lòng chọn ít nhất 1 ngày'); return; }
    
    const newData = {};
    selEmps.forEach(ec => {
        newData[ec.value] = { name:ec.dataset.name, days:{} };
        selDays.forEach(dc => { newData[ec.value].days[dc.value] = bulkAllData[ec.value]?.days[dc.value] || { label:dc.dataset.label }; });
        if (!bulkTableRows[ec.value]) bulkTableRows[ec.value] = [];
    });
    bulkAllData = newData;
    
    // Tabs
    const tabsEl = document.getElementById('bulkEmpTabs'); tabsEl.innerHTML='';
    selEmps.forEach((ec,idx)=>{
        const tab=document.createElement('button'); tab.type='button';
        tab.className='bulk-emp-tab'+(idx===0?' active':'');
        tab.textContent=ec.dataset.name; tab.dataset.empId=ec.value;
        tab.onclick=()=>switchBulkEmp(ec.value); tabsEl.appendChild(tab);
    });
    document.getElementById('bulkSummary').textContent=`${selEmps.length} người × ${selDays.length} ngày`;
    
    bulkActiveEmpId = selEmps[0].value;
    renderBulkTable(bulkActiveEmpId);
    document.getElementById('bulkInputPopup').classList.remove('hidden');
    document.getElementById('bulkInputPopup').classList.add('flex');
}

function getBulkDates(empId) {
    return Object.keys(bulkAllData[empId]?.days||{}).map(date=>({ date, label:bulkAllData[empId].days[date].label }));
}

function saveBulkRows(empId) {
    const tbody = document.getElementById(`bulk-tbody-${empId}`);
    if (!tbody || !bulkTableRows[empId]) return;
    tbody.querySelectorAll('tr[data-row-id]').forEach(tr => {
        const row = bulkTableRows[empId].find(r=>r.id===parseInt(tr.dataset.rowId));
        if (!row) return;
        row.phase = tr.querySelector('.row-phase')?.value||'';
        tr.querySelectorAll('.hours-cell').forEach(inp=>{ row.hours[inp.dataset.date]=inp.value; });
    });
}

function renderBulkTable(empId) {
    const container = document.getElementById('bulkInputBlocks');
    container.innerHTML = '';
    const dates = getBulkDates(empId);
    if (!dates.length) return;
    if (!bulkTableRows[empId]) bulkTableRows[empId]=[];
    
    // Ensure all rows have all date keys
    bulkTableRows[empId].forEach(row=>{ dates.forEach(d=>{ if(!(d.date in row.hours)) row.hours[d.date]=''; }); });
    
    // Create wrapper div for buildTickTableUI
    const wrap = document.createElement('div'); wrap.id=`bulk-wrap-${empId}`;
    container.appendChild(wrap);
    
    buildTickTableUI(`bulk-wrap-${empId}`, dates, bulkTableRows[empId], {
        dropdownId: `bulk-ms-dd-${empId}`,
        tbodyId: `bulk-tbody-${empId}`,
        onAddRow: (projData) => {
            saveBulkRows(empId);
            bulkTableRows[empId].push({ id:bulkRowCtr++, projectId:projData.projectId, projectName:projData.projectName, customer:projData.customer, phase:'', hours:Object.fromEntries(dates.map(d=>[d.date,''])) });
            renderBulkTable(empId);
        },
        onRemoveRow: (idOrProjectId, type) => {
            saveBulkRows(empId);
            if (type==='byProjectId') {
                const lastIdx=[...bulkTableRows[empId]].reverse().findIndex(r=>String(r.projectId)===String(idOrProjectId));
                if(lastIdx>=0) bulkTableRows[empId].splice(bulkTableRows[empId].length-1-lastIdx,1);
            } else {
                const row=bulkTableRows[empId].find(r=>r.id===idOrProjectId);
                bulkTableRows[empId]=bulkTableRows[empId].filter(r=>r.id!==idOrProjectId);
                if(row){
                    const still=bulkTableRows[empId].some(r=>String(r.projectId)===String(row.projectId));
                    if(!still){const dd=document.getElementById(`bulk-ms-dd-${empId}`);if(dd){const cb=dd.querySelector(`input[value="${row.projectId}"]`);if(cb){cb.checked=false;cb.closest('.ms-item').style.background='';}}}
                }
            }
            renderBulkTable(empId);
        }
    });
}

function switchBulkEmp(empId) {
    saveBulkRows(bulkActiveEmpId);
    bulkActiveEmpId = empId;
    document.querySelectorAll('.bulk-emp-tab').forEach(t=>t.classList.toggle('active',t.dataset.empId===empId));
    renderBulkTable(empId);
}

function saveBulkCurrentData() { saveBulkRows(bulkActiveEmpId); }

function closeBulkInputPopup() {
    saveBulkRows(bulkActiveEmpId);
    document.getElementById('bulkInputPopup').classList.add('hidden');
    document.getElementById('bulkInputPopup').classList.remove('flex');
}

function copyFirstDayToAllMode3() {
    saveBulkRows(bulkActiveEmpId);
    const empId = bulkActiveEmpId;
    const dates = getBulkDates(empId);
    if (dates.length<=1) { showErrorModal('Cần có ít nhất 2 ngày để copy'); return; }
    const firstDate = dates[0].date;
    const rows = bulkTableRows[empId]||[];
    const hasData = rows.some(r=>parseFloat(r.hours[firstDate])>0);
    if (!hasData) { showErrorModal('Vui lòng nhập giờ cho cột ngày đầu tiên trước'); return; }
    rows.forEach(row=>{ dates.slice(1).forEach(d=>{ row.hours[d.date]=row.hours[firstDate]; }); });
    renderBulkTable(empId);
    showSuccessModal(`✓ Đã copy giờ từ ngày đầu cho ${dates.length-1} ngày còn lại!`);
}

// ============================================================
// SUBMIT HANDLERS
// ============================================================
function handleSubmitMode1() {
    const department=document.getElementById('department1').value; const employee=document.getElementById('employee1').value; const day=document.getElementById('day1').value;
    if(!department){showErrorModal('Vui lòng chọn bộ phận');return;} if(!employee){showErrorModal('Vui lòng chọn nhân viên');return;} if(!day){showErrorModal('Vui lòng chọn ngày');return;}
    const container=document.getElementById('projectRows1'); const rows=[]; let err='';
    container.querySelectorAll('.bulk-project-row').forEach((r,i)=>{
        const cust=r.querySelector('.bulk-customer').value,proj=r.querySelector('.bulk-project').value,phase=r.querySelector('.bulk-pp').value,hrs=parseFloat(r.querySelector('.bulk-hours-input').value);
        if(!cust||!proj||!phase||!hrs||hrs<=0) err=`Vui lòng nhập đầy đủ (dòng ${i+1})`;
        rows.push({Customer:cust,ProjectId:parseInt(proj),ProjectPhase:phase,WorkHours:hrs});
    });
    if(err){showErrorModal(err);return;} if(!rows.length){showErrorModal('Vui lòng thêm ít nhất 1 dự án');return;}
    if(rows.reduce((s,r)=>s+r.WorkHours,0)>24){showErrorModal('Tổng giờ vượt quá 24h');return;}
    const empName=document.getElementById('employee1').selectedOptions[0]?.text||'';
    showConfirmModal(`Xác nhận lưu ${rows.length} dự án cho ${empName}?`, function(){
        fetch(`${pathBase}/Heatmap/SaveStaffDetailMulti`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({EmployeeId:parseInt(employee),WorkDate:day,Projects:rows})})
        .then(r=>r.json()).then(res=>{
            if(res.success){showSuccessModal(typeof t==="function"?t('import.success.save1').replace('{n}',rows.length):`✓ Saved ${rows.length}!`);
            if(!window.userDepartmentId)document.getElementById('department1').selectedIndex=0;
            document.getElementById('employee1').innerHTML='<option value="">-- Chọn bộ phận trước --</option>';resetSearchableSelect('employee1-sd','-- Chọn bộ phận trước --');
            if(window.userDepartmentId)loadEmployees(String(window.userDepartmentId),'employee1');
            document.getElementById('day1').value='';initMode1ProjectRows();
        }else showErrorModal('Lỗi: '+(res.message||'Không thể lưu'));
    }).catch(e=>showErrorModal('Lỗi kết nối: '+e.message));
});
}

function handleSubmitMode2() {
    const employee = document.getElementById('employee2').value;
    const dates = getMode2Dates();
    if (!employee) { showErrorModal(t('import.err.no_emp2')); return; }
    if (!dates.length) { showErrorModal(t('import.err.no_day')); return; }
    
    saveMode2Rows();
    
    // ── Bước 1: Validate từng hàng có giờ nhập ─────────────────────────
    for (let idx = 0; idx < mode2Rows.length; idx++) {
        const row = mode2Rows[idx];
        const hasAnyHours = dates.some(d => parseFloat(row.hours[d.date]) > 0);
        if (!hasAnyHours) continue;
        
        if (!row.projectId) {
            showErrorModal(`${t('import.err.row')||'Hàng'} ${idx + 1}: ${t('import.err.no_proj')||'Chưa chọn dự án'}`); return;
        }
        // Đọc phase từ DOM trực tiếp để chắc chắn (không phụ thuộc state cache)
        const phaseEl = document.querySelector(`#mode2-tbody tr[data-row-id="${row.id}"] .row-phase`);
        const phaseVal = phaseEl ? phaseEl.value : row.phase;
        if (!phaseVal) {
            showErrorModal(`${t('import.err.row')||'Hàng'} ${idx + 1} (${row.projectName}): ${t('import.err.no_phase')||'Vui lòng chọn Project Phase'}`); return;
        }
    }
    
    // ── Bước 2: Gom dữ liệu — chỉ lấy ô có giờ > 0 ────────────────────
    const dayMap = {};
    mode2Rows.forEach(row => {
        dates.forEach(d => {
            const hrs = parseFloat(row.hours[d.date]);
            if (!hrs || hrs <= 0) return; // bỏ qua ô trống
            if (!dayMap[d.date]) dayMap[d.date] = [];
            dayMap[d.date].push({ Customer: row.customer, ProjectId: parseInt(row.projectId), ProjectPhase: row.phase, WorkHours: hrs });
        });
    });
    
    for (const [date, projs] of Object.entries(dayMap)) {
        if (projs.reduce((s, p) => s + p.WorkHours, 0) > 24) {
            showErrorModal(`${t('import.err.over24h')||'Tổng giờ vượt quá 24h'} (${date})`); return;
        }
    }
    
    const days = Object.entries(dayMap).map(([date, projs]) => {
        const p = date.split('/'); return { Date: `${p[2]}-${p[1]}-${p[0]}`, Projects: projs };
    });
    if (!days.length) { showErrorModal(t('import.err.no_hours')||'Vui lòng nhập giờ cho ít nhất 1 ô'); return; }
    
    const empName = document.getElementById('employee2').selectedOptions[0]?.text || '';
    showConfirmModal(`Xác nhận lưu cho ${empName} - ${days.length} ngày?`, function() {
        fetch(`${pathBase}/Heatmap/SaveMultipleDaysMulti`, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({ EmployeeId:parseInt(employee), Days:days }) })
        .then(r => r.json()).then(res => {
            if (res.success) {
                showSuccessModal(typeof t==="function" ? t('import.success.save2').replace('{n}',days.length) : `✓ Saved ${days.length} days!`);
                const d2 = window.userDepartmentId ? String(window.userDepartmentId) : document.getElementById('department2').value;
                if (d2) loadEmployees(d2, 'employee2');
                else { document.getElementById('employee2').innerHTML='<option value="">-- Chọn bộ phận trước --</option>'; resetSearchableSelect('employee2-sd','-- Chọn bộ phận trước --'); }
                const w2 = document.getElementById('week2'); if (w2?.value) w2.dispatchEvent(new Event('change'));
                document.getElementById('dayHoursSection2').style.display = 'none';
                mode2Rows = []; mode2RowCtr = 0;
            } else showErrorModal(res.message || 'Có lỗi xảy ra');
        }).catch(e => showErrorModal('Lỗi kết nối: ' + e.message));
    });
}

function handleSubmitMode3() {
    saveBulkRows(bulkActiveEmpId);
    const records = []; let err = '';
    
    // ── Validate tất cả employee × row ──────────────────────────────────
    outer:
    for (const [empId, empData] of Object.entries(bulkAllData)) {
        const rows = bulkTableRows[empId] || [];
        const dates = getBulkDates(empId);
        for (let idx = 0; idx < rows.length; idx++) {
            const row = rows[idx];
            const hasAnyHours = dates.some(d => parseFloat(row.hours[d.date]) > 0);
            if (!hasAnyHours) continue;
            
            if (!row.projectId) { err = `${t('import.err.row')||'Hàng'} ${idx+1} (${empData.name}): ${t('import.err.no_proj')||'Chưa chọn dự án'}`; break outer; }
            const phaseEl3 = document.querySelector(`#bulk-tbody-${empId} tr[data-row-id="${row.id}"] .row-phase`);
            const phaseVal3 = phaseEl3 ? phaseEl3.value : row.phase;
            if (!phaseVal3) { err = `${t('import.err.row')||'Hàng'} ${idx+1} (${empData.name} - ${row.projectName}): ${t('import.err.no_phase')||'Vui lòng chọn Project Phase'}`; break outer; }
        }
    }
    if (err) { showErrorModal(err); return; }
    
    // ── Gom dữ liệu — chỉ lấy ô có giờ > 0 ─────────────────────────────
    Object.entries(bulkAllData).forEach(([empId, empData]) => {
        const rows = bulkTableRows[empId] || [];
        const dates = getBulkDates(empId);
        rows.forEach(row => {
            dates.forEach(d => {
                const hrs = parseFloat(row.hours[d.date]);
                if (!hrs || hrs <= 0) return; // bỏ qua ô trống
                const p = d.date.split('/');
                records.push({ EmpId:parseInt(empId), Date:`${p[2]}-${p[1]}-${p[0]}`, Customer:row.customer, ProjectId:parseInt(row.projectId), ProjectPhase:row.phase, Hours:hrs });
            });
        });
    });
    
    if (!records.length) { showErrorModal(t('import.err.no_hours')||'Vui lòng nhập giờ cho ít nhất 1 ô'); return; }
    
    // ── Kiểm tra tổng giờ/người/ngày ≤ 24h ──────────────────────────────
    const hc = {};
    records.forEach(r => { const k=`${r.EmpId}_${r.Date}`; hc[k]=(hc[k]||0)+r.Hours; });
    const over = Object.entries(hc).find(([, h]) => h > 24);
    if (over) { showErrorModal(t('import.err.over24h_check')||'Tổng giờ trong 1 ngày vượt quá 24h'); return; }
    
    showConfirmModal(`Bạn sắp tạo ${records.length} bản ghi. Xác nhận lưu?`, async function() {
        try {
            const res = await fetch(`${pathBase}/Heatmap/BulkImportMultiProject`, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(records) });
            const result = await res.json();
            if (result.success) {
                closeBulkInputPopup();
                showSuccessModal(typeof t==="function" ? t('import.success.save3').replace('{n}',result.total) : `✅ Saved ${result.total} records!`);
                bulkAllData = {}; bulkTableRows = {}; bulkActiveEmpId = null;
                document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]').forEach(cb => { cb.checked=false; cb.closest('.employee-checkbox')?.classList.remove('selected'); });
                document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]').forEach(cb => { cb.checked=false; cb.closest('.day-checkbox')?.classList.remove('selected'); });
            } else showErrorModal('Lỗi: ' + (result.message || 'Không thể lưu'));
        } catch(e) { showErrorModal('Lỗi kết nối: ' + e.message); }
    });
}

// ============================================================
// MODALS
// ============================================================
function showErrorModal(m){document.getElementById('errorModalMessage').textContent=m;document.getElementById('errorModal').classList.remove('hidden');document.getElementById('errorModal').classList.add('flex');}
function closeErrorModal(){document.getElementById('errorModal').classList.add('hidden');document.getElementById('errorModal').classList.remove('flex');}
function showSuccessModal(m){document.getElementById('successModalMessage').textContent=m;const ml=document.getElementById('successModal');ml.classList.remove('hidden');ml.classList.add('flex');if(typeof applyI18n==='function')applyI18n();}
function closeSuccessModal(){const ml=document.getElementById('successModal');ml.classList.add('hidden');ml.classList.remove('flex');}
function showConfirmModal(m,cb){confirmCallback=cb;document.getElementById('confirmMessage').textContent=m;document.getElementById('confirmModal').classList.remove('hidden');document.getElementById('confirmModal').classList.add('flex');}
function closeConfirmModal(){document.getElementById('confirmModal').classList.add('hidden');document.getElementById('confirmModal').classList.remove('flex');confirmCallback=null;}
function confirmSubmit(){if(confirmCallback)confirmCallback();closeConfirmModal();}

// ============================================================
// HELPERS
// ============================================================
function getWeekNumber(date){const d=new Date(Date.UTC(date.getFullYear(),date.getMonth(),date.getDate()));const dn=d.getUTCDay()||7;d.setUTCDate(d.getUTCDate()+4-dn);const ys=new Date(Date.UTC(d.getUTCFullYear(),0,1));return Math.ceil((((d-ys)/86400000)+1)/7);}
function getMonday(date){const d=new Date(date);const day=d.getDay();const diff=d.getDate()-day+(day===0?-6:1);return new Date(d.setDate(diff));}
function formatDate(date){const d=new Date(date);return `${String(d.getDate()).padStart(2,'0')}/${String(d.getMonth()+1).padStart(2,'0')}/${d.getFullYear()}`;}

// ── Re-render table khi đổi ngôn ngữ ────────────────────────────────────────
document.addEventListener('i18n:applied', function() {
    // ── Restore searchable select display texts ────────────────────────
    // Sau khi applyI18n chạy, một số thứ có thể đã overwrite display text.
    // Đọc lại từ hidden <select> để đảm bảo hiển thị đúng tên đã chọn.
    document.querySelectorAll('.searchable-select[data-target]').forEach(sd => {
        const targetId = sd.getAttribute('data-target');
        const hiddenSel = document.getElementById(targetId);
        if (!hiddenSel || !hiddenSel.value) return;
        
        const selectedOpt = hiddenSel.options[hiddenSel.selectedIndex];
        if (!selectedOpt || !selectedOpt.value) return; // value='' là placeholder
        
        const display = sd.querySelector('.searchable-select-display');
        if (!display) return;
        
        display.textContent = selectedOpt.text;
        display.classList.add('selected');
    });
    // Mode 2: re-render nếu section đang hiện để cập nhật header cột
    const section2 = document.getElementById('dayHoursSection2');
    if (section2 && section2.style.display !== 'none') {
        // Dùng setTimeout để chạy SAU khi listener restore checked dates đã xong
        setTimeout(() => {
            if (getMode2Dates().length > 0) {
                saveMode2Rows();
                renderMode2Section();
            }
        }, 0);
    }
    
    // Mode 3: re-render tab đang active nếu popup đang mở
    const popup = document.getElementById('bulkInputPopup');
    if (popup && !popup.classList.contains('hidden') && bulkActiveEmpId) {
        setTimeout(() => {
            saveBulkRows(bulkActiveEmpId);
            renderBulkTable(bulkActiveEmpId);
        }, 0);
    }
});
