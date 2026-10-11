(() => {
    'use strict';
    const page = document.body.dataset.adminPage;
    const root = document.getElementById('adminContent');
    const key = 'smartbus-admin-demo-v1';
    const money = value => new Intl.NumberFormat('vi-VN').format(value) + ' đ';
    const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const seed = {
        routes: [{id:'TN-HN',name:'Thái Nguyên → Hà Nội',price:80000,status:'Hoạt động',stops:'Thái Nguyên → Sóc Sơn → Mỹ Đình'}, {id:'HN-HP',name:'Hà Nội → Hải Phòng',price:150000,status:'Hoạt động',stops:'Giáp Bát → Hải Dương → Hải Phòng'}],
        stops: ['Thái Nguyên','Sóc Sơn','Mỹ Đình','Giáp Bát','Hải Dương','Hải Phòng'],
        schedules: [{id:'TN-HN-01',route:'TN-HN',date:'2026-10-01',start:'08:00',end:'10:00',days:['2','3','4','5','6','7','8']},{id:'TN-HN-02',route:'TN-HN',date:'2026-10-01',start:'10:00',end:'12:00',days:['2','3','4','5','6','7','8']},{id:'TN-HN-03',route:'TN-HN',date:'2026-10-01',start:'14:00',end:'16:00',days:['2','3','4','5','6']}],
        dispatch: [{id:'TN-HN-01',vehicle:'29B-888.68',driver:'Trần Đình Trọng',assistant:'Nguyễn Quốc Minh'},{id:'TN-HN-02',vehicle:'',driver:'',assistant:''}],
        vouchers: [{id:'SMARTBUSNEW',type:'amount',value:20000,start:'2026-10-01',end:'2026-10-31',minimum:100000,limit:100,used:18},{id:'SINHVIEN',type:'percent',value:10,start:'2026-10-01',end:'2026-10-31',minimum:50000,limit:200,used:42}],
        approvals: [{id:'HS-001',name:'Nguyễn Văn An',category:'Sinh viên',school:'ICTU',date:'2026-09-30',status:'Chờ duyệt',note:''},{id:'HS-002',name:'Lê Thị Mai',category:'Người cao tuổi',school:'',date:'2026-09-30',status:'Cần bổ sung',note:'Cần bổ sung giấy tờ chứng minh đối tượng.'}],
        support: [{id:'PA-001',name:'Nguyễn Văn An',subject:'Chuyến trễ giờ',description:'Xe đến trễ so với giờ dự kiến, cần cập nhật thời gian đón.',status:'Đang xử lý',reply:''},{id:'PA-002',name:'Trần Văn Bình',subject:'Thắc mắc đổi vé',description:'Tôi muốn đổi chuyến sang ngày mai, cần thực hiện những bước nào?',status:'Mới tiếp nhận',reply:''}],
        accounts: [{id:'USER-001',name:'Nguyễn Văn An',email:'an@example.com',role:'CUSTOMER',status:'Hoạt động'},{id:'DRIVER-002',name:'Trần Đình Trọng',email:'trong@example.com',role:'DRIVER',status:'Hoạt động'},{id:'MANAGER-003',name:'Nguyễn Khánh Tùng',email:'tung@example.com',role:'MANAGER',status:'Hoạt động'}],
        activity: [{time:'07:45:10',date:'2026-10-01',email:'manager@example.com',action:'Cập nhật lịch trình',target:'TN-HN-01'},{time:'07:50:22',date:'2026-10-01',email:'admin@example.com',action:'Phân quyền tài khoản',target:'DRIVER-002'},{time:'07:55:00',date:'2026-10-01',email:'trong@example.com',action:'Soát vé QR',target:'SBG-001-A01'}]
    };
    let state = structuredClone(seed), storageAvailable = true;
    try {
        const saved = JSON.parse(localStorage.getItem(key) || 'null');
        if (saved && Object.keys(seed).every(k => Array.isArray(saved[k]))) state = saved;
    } catch { storageAvailable = false; }
    let selected = {routes:state.routes[0]?.id, schedules:null, dispatch:'TN-HN-02', vouchers:'SMARTBUSNEW', approvals:'HS-001', support:'PA-001', accounts:null};
    const filters = {search:'',route:'TN-HN',date:'2026-10-01',dashboardRoute:'all',from:'2026-10-01',to:'2026-10-31',logDate:'',logEmail:'',logAction:''};
    const roleLabels = {CUSTOMER:'Hành khách',DRIVER:'Tài xế',MANAGER:'Quản lý',ADMIN:'Admin'};
    const subtitles = {dashboard:'01/10/2026 · Dữ liệu minh họa',routes:'Thiết lập lộ trình, thứ tự trạm và giá vé.',schedules:'Thiết lập thời gian biểu theo tuyến và ngày chạy.',dispatch:'Gán phương tiện, tài xế và phụ xe cho từng chuyến.',vouchers:'Marketing · Thiết lập ưu đãi và theo dõi sử dụng.',approvals:'Đối chiếu giấy tờ và phản hồi kết quả xét duyệt.',support:'Tiếp nhận phản ánh và theo dõi tiến độ giải quyết.',accounts:'Admin · Mô phỏng vai trò hiển thị của tài khoản.',activity:'Admin · Tra cứu truy cập và thao tác hệ thống.'};
    document.getElementById('adminSubtitle').textContent = subtitles[page] || '';
    const dateText = date => date ? date.split('-').reverse().join('/') : '—';
    const options = (values, value, placeholder) => (placeholder ? `<option value="">${esc(placeholder)}</option>` : '') + values.map(v => { const [id,label] = Array.isArray(v) ? v : [v,v]; return `<option value="${esc(id)}"${String(value)===String(id)?' selected':''}>${esc(label)}</option>`; }).join('');
    const routeOptions = value => options(state.routes.map(r=>[r.id,r.name]),value);
    const input = (name,label,value='',type='text',extra='',id=`ad-${name}`) => `<div class="ad-field"><label for="${esc(id)}">${label}</label><input id="${esc(id)}" name="${esc(name)}" type="${type}" value="${esc(value)}" ${extra}></div>`;
    const select = (name,label,values,value,placeholder) => `<div class="ad-field"><label for="ad-${name}">${label}</label><select id="ad-${name}" name="${name}">${options(values,value,placeholder)}</select></div>`;
    const primary = (text, attrs='') => `<button class="ad-button primary" ${attrs}>${text}</button>`;
    const table = (headers,rows) => `<div class="ad-card ad-table-wrap"><table><thead><tr>${headers.map(h=>`<th scope="col">${h}</th>`).join('')}</tr></thead><tbody>${rows.length ? rows.map(row=>`<tr>${row.map(c=>`<td>${c}</td>`).join('')}</tr>`).join('') : `<tr><td class="ad-empty" colspan="${headers.length}">Không có dữ liệu phù hợp.</td></tr>`}</tbody></table></div>`;
    const rowButton = (group,id,text) => `<button class="ad-row-button" type="button" data-select="${group}" data-id="${esc(id)}" aria-label="Chọn ${esc(text)}">${esc(text)}</button>`;
    const get = group => state[group].find(item=>item.id===selected[group]);
    const notify = (text,error=false) => { const box=document.getElementById('adminFeedback');box.textContent=text;box.dataset.error=String(error);box.hidden=false;clearTimeout(notify.timer);notify.timer=setTimeout(()=>box.hidden=true,5500); };
    function persist(action,target) {
        const now=new Date();
        const date=[now.getFullYear(),String(now.getMonth()+1).padStart(2,'0'),String(now.getDate()).padStart(2,'0')].join('-');
        state.activity.unshift({date,time:now.toLocaleTimeString('vi-VN',{hour12:false}),email:'admin@example.com',action,target});
        state.activity=state.activity.slice(0,300);
        try {localStorage.setItem(key,JSON.stringify(state));storageAvailable=true;} catch {storageAvailable=false;}
        render(); notify(storageAvailable?'Đã lưu dữ liệu demo trên trình duyệt.':'Đã cập nhật demo trong bộ nhớ; trình duyệt không cho phép lưu lâu dài.',!storageAvailable);
    }
    const renderers = {
        dashboard() {
            const daily=[1000000,1500000,1900000,2300000,2800000,1200000,1700000];
            const rows=[{id:'TN-HN',name:'Thái Nguyên–Hà Nội',revenue:6800000,tickets:100,fill:78},{id:'HN-HP',name:'Hà Nội–Hải Phòng',revenue:5600000,tickets:86,fill:65}].filter(r=>filters.dashboardRoute==='all'||r.id===filters.dashboardRoute);
            const days=daily.map((amount,i)=>({date:`2026-10-0${i+1}`,amount})).filter(d=>d.date>=filters.from&&d.date<=filters.to);
            const ratio=days.reduce((sum,d)=>sum+d.amount,0)/12400000;
            const revenue=Math.round(rows.reduce((s,r)=>s+r.revenue,0)*ratio), tickets=Math.round(rows.reduce((s,r)=>s+r.tickets,0)*ratio);
            const fill=rows.length ? Math.round(rows.reduce((s,r)=>s+r.fill,0)/rows.length) : 0;
            return `<div class="ad-fields three" style="margin-bottom:22px"><div class="ad-field"><label>Khoảng thời gian</label><div class="ad-fields" style="gap:6px"><input aria-label="Từ ngày" type="date" data-filter="from" value="${filters.from}"><input aria-label="Đến ngày" type="date" data-filter="to" value="${filters.to}"></div></div><div class="ad-field"><label for="reportRoute">Tuyến</label><select id="reportRoute" data-filter="dashboardRoute">${options([['all','Tất cả tuyến'],...state.routes.map(r=>[r.id,r.name])],filters.dashboardRoute)}</select></div><div class="ad-export-actions"><button type="button" class="ad-button" data-action="export">Xuất Excel (CSV)</button><button type="button" class="ad-button" data-action="print">PDF</button></div></div><div class="ad-kpis">${[['Doanh thu',money(revenue)],['Vé đã bán',tickets+' vé'],['Tỷ lệ lấp đầy',fill+'%'],['Chuyến hôm nay','24 chuyến']].map(([label,value])=>`<article class="ad-card ad-kpi"><span>${label}</span><strong>${value}</strong></article>`).join('')}</div><section class="ad-card"><h2>Doanh thu theo ngày</h2><div class="ad-chart" role="img" aria-label="Biểu đồ doanh thu minh họa từ ngày 1 đến 7 tháng 10">${daily.map((amount,i)=>{const visible=days.some(d=>d.date===`2026-10-0${i+1}`);const value=visible?Math.round(amount*(filters.dashboardRoute==='all'?1:rows.reduce((s,r)=>s+r.revenue,0)/12400000)):0;return `<div class="ad-chart-column"><div class="ad-bar" style="height:${value/2800000*100}%" data-value="${money(value)}" title="${money(value)}"></div><span class="ad-chart-label">0${i+1}/10</span></div>`;}).join('')}</div></section>${table(['Tuyến','Doanh thu','Vé','Lấp đầy'],rows.map(r=>[esc(r.name),money(Math.round(r.revenue*ratio)),Math.round(r.tickets*ratio),r.fill+'%']))}`;
        },
        routes() {
            const item=get('routes')||{id:'',name:'',price:'',stops:''};
            const found=state.routes.filter(r=>(r.id+' '+r.name).toLocaleLowerCase('vi').includes(filters.search.toLocaleLowerCase('vi')));
            return `<div class="ad-toolbar"><div class="ad-field"><label for="routeSearch">Tìm tuyến</label><input id="routeSearch" type="search" data-filter="search" value="${esc(filters.search)}" placeholder="Nhập tên / mã tuyến"></div>${primary('Thêm tuyến','type="button" data-action="new-route"')}<button class="ad-button" type="button" data-action="new-stop">Thêm trạm</button></div>${table(['Mã tuyến','Tên tuyến','Giá vé','Trạng thái'],found.map(r=>[rowButton('routes',r.id,r.id),esc(r.name),money(r.price),esc(r.status)]))}<form class="ad-card" data-form="routes"><div class="ad-fields">${!item.id?input('id','Mã tuyến','','text','required maxlength="20"'):''}${input('name','Tên tuyến',item.name,'text','required maxlength="120"')}${input('price','Giá vé',item.price,'number','required min="1000" step="1000"')}<div class="ad-wide">${input('stops','Thứ tự trạm',item.stops,'text','required maxlength="250" list="adminStops"')}<datalist id="adminStops">${state.stops.map(s=>`<option value="${esc(s)}">`).join('')}</datalist></div></div><div class="ad-actions">${primary('Lưu thông tin tuyến','type="submit"')}</div></form>`;
        },
        schedules() {
            const item=get('schedules')||{start:'08:00',end:'10:00',days:['2','3','4','5','6','7','8']};
            const rows=state.schedules.filter(s=>s.route===filters.route&&s.date===filters.date);
            return `<div class="ad-fields" style="margin-bottom:22px"><div class="ad-field"><label for="scheduleRoute">Tuyến</label><select id="scheduleRoute" data-filter="route">${routeOptions(filters.route)}</select></div><div class="ad-field"><label for="scheduleDate">Ngày</label><input id="scheduleDate" data-filter="date" type="date" value="${filters.date}"></div></div>${table(['Mã chuyến','Giờ đi / đến','Tần suất','Trạng thái'],rows.map(s=>[rowButton('schedules',s.id,s.id),s.start+' / '+s.end,s.days.length===7?'Hằng ngày':s.days.map(d=>d==='8'?'CN':'Thứ '+d).join(', '),'Đã lên lịch']))}<form class="ad-card" data-form="schedules"><div class="ad-fields three">${input('start','Giờ khởi hành',item.start,'time','required')}${input('end','Giờ đến dự kiến',item.end,'time','required')}<div class="ad-field"><label>Ngày áp dụng</label><div class="ad-days">${['2','3','4','5','6','7','8'].map(d=>`<label class="ad-day"><input type="checkbox" name="days" value="${d}" ${item.days.includes(d)?'checked':''}>${d==='8'?'CN':'T'+d}</label>`).join('')}</div></div></div><div class="ad-actions">${primary(selected.schedules?'Cập nhật lịch trình':'Tạo lịch trình','type="submit"')}${selected.schedules?'<button class="ad-button" type="button" data-action="new-schedule">Tạo mới</button>':''}</div></form>`;
        },
        dispatch() {
            const item=get('dispatch')||{id:state.schedules[0]?.id,vehicle:'',driver:'',assistant:''};
            const label=id=>{const s=state.schedules.find(s=>s.id===id);return id+(s?' · '+s.start:'');};
            return `${table(['Chuyến','Phương tiện','Tài xế','Phụ xe'],state.dispatch.map(d=>[rowButton('dispatch',d.id,label(d.id)),esc(d.vehicle||'Chưa phân công'),esc(d.driver||'Chưa phân công'),esc(d.assistant||'Chưa phân công')]))}<form class="ad-card" data-form="dispatch"><div class="ad-fields"><div class="ad-wide">${select('trip','Chuyến',state.schedules.map(s=>[s.id,label(s.id)]),item.id)}</div></div><div class="ad-fields three" style="margin-top:16px">${select('vehicle','Xe',['29B-888.68','20B-123.45','15B-456.78'],item.vehicle,'Chọn xe khả dụng')}${select('driver','Tài xế',['Trần Đình Trọng','Nguyễn Văn Tuấn','Lê Văn Hùng'],item.driver,'Chọn tài xế')}${select('assistant','Phụ xe',['Nguyễn Quốc Minh','Phạm Văn Hải','Trần Thị Hoa'],item.assistant,'Chọn phụ xe')}</div><div class="ad-note">Kiểm tra trùng lịch trước khi lưu phân công.</div><div class="ad-actions">${primary('Lưu phân công','type="submit"')}</div></form>`;
        },
        vouchers() {
            const item=get('vouchers')||{id:'',type:'amount',value:20000,start:'2026-10-01',end:'2026-10-31',minimum:0,limit:100};
            return `${table(['Mã','Giảm giá','Hiệu lực','Đã dùng'],state.vouchers.map(v=>[rowButton('vouchers',v.id,v.id),v.type==='amount'?money(v.value):v.value+'%',dateText(v.start)+'–'+dateText(v.end),v.used+' / '+v.limit]))}<form class="ad-card" data-form="vouchers"><div class="ad-fields">${input('id','Mã voucher',item.id,'text','required maxlength="30" pattern="[A-Za-z0-9_-]+"')}${select('type','Loại giảm giá',[['amount','Số tiền'],['percent','Phần trăm']],item.type)}${input('value','Giá trị giảm',item.value,'number','required min="1"')}${input('minimum','Đơn tối thiểu (đ)',item.minimum,'number','required min="0"')}${input('start','Ngày bắt đầu',item.start,'date','required')}${input('end','Ngày kết thúc',item.end,'date','required')}${input('limit','Giới hạn lượt sử dụng',item.limit,'number','required min="1" step="1"')}</div><div class="ad-actions">${primary('Lưu voucher','type="submit"')}<button class="ad-button" type="button" data-action="new-voucher">Tạo mã mới</button></div></form>`;
        },
        approvals() {
            const item=get('approvals')||state.approvals[0];
            return `${table(['Hành khách','Đối tượng','Ngày gửi','Trạng thái'],state.approvals.map(a=>[rowButton('approvals',a.id,a.name),esc(a.category),dateText(a.date),esc(a.status)]))}<div class="ad-two-columns"><article class="ad-card"><h2>${esc(item.name)}</h2><div class="ad-details">Đối tượng: ${esc(item.category)}<br>${item.school?'Trường: '+esc(item.school)+'<br>':''}Hồ sơ: Ảnh giấy tờ đã tải lên<br>Trạng thái: ${esc(item.status)}</div><button class="ad-button" type="button" data-action="attachment" style="width:100%">Xem tệp đính kèm</button></article><form class="ad-card" data-form="approvals"><div class="ad-field"><label for="approvalNote">Ghi chú / lý do</label><textarea id="approvalNote" name="note" placeholder="Nhập nội dung phản hồi" maxlength="500">${esc(item.note)}</textarea></div><div class="ad-stack" style="margin-top:16px">${primary('Duyệt hồ sơ','type="submit" name="decision" value="Đã duyệt"')}<button class="ad-button" type="submit" name="decision" value="Cần bổ sung">Yêu cầu bổ sung</button><button class="ad-button" type="submit" name="decision" value="Từ chối">Từ chối</button></div></form></div>`;
        },
        support() {
            const item=get('support')||state.support[0];
            return `${table(['Mã phản ánh','Hành khách','Nội dung','Trạng thái'],state.support.map(s=>[rowButton('support',s.id,s.id),esc(s.name),esc(s.subject),esc(s.status)]))}<form class="ad-card" data-form="support"><h2>${esc(item.id)} · ${esc(item.subject)}</h2><p class="ad-details">Hành khách: ${esc(item.description)}</p><div class="ad-fields"><div class="ad-wide ad-field"><label for="supportReply">Phản hồi</label><textarea id="supportReply" name="reply" maxlength="1000" required placeholder="Nhập nội dung trả lời hành khách">${esc(item.reply)}</textarea></div><div class="ad-wide">${select('status','Trạng thái xử lý',['Mới tiếp nhận','Đang xử lý','Đã giải quyết'],item.status)}</div></div><div class="ad-actions">${primary('Lưu phản hồi','type="submit"')}</div></form>`;
        },
        accounts() {
            const item=get('accounts')||{name:'',email:'',role:'CUSTOMER'};
            return `${table(['Họ tên','Tài khoản','Vai trò','Trạng thái'],state.accounts.map(a=>[rowButton('accounts',a.id,a.name),esc(a.email),roleLabels[a.role]||esc(a.role),esc(a.status)]))}<form class="ad-card" data-form="accounts"><div class="ad-fields">${input('name','Họ tên',item.name,'text','required maxlength="100" placeholder="Nhập họ tên"')}${input('email','Email',item.email,'email','required maxlength="150" placeholder="Nhập email"')}<div class="ad-wide">${select('role','Vai trò',Object.keys(roleLabels).map(r=>[r,r+' · '+roleLabels[r]]),item.role)}</div></div><div class="ad-actions">${primary('Lưu tài khoản và vai trò mẫu','type="submit"')}<button class="ad-button" type="button" data-action="new-account">Thêm tài khoản</button></div></form><div class="ad-note">Quyền Marketing / CSKH cần thống nhất với nhóm theo ma trận RBAC. Lựa chọn tại đây chỉ thay đổi dữ liệu demo, không cấp quyền truy cập thật.</div>`;
        },
        activity() {
            const logs=state.activity.filter(l=>(!filters.logDate||l.date===filters.logDate)&&(!filters.logEmail||l.email===filters.logEmail)&&(!filters.logAction||l.action===filters.logAction));
            return `<div class="ad-fields three" style="margin-bottom:22px"><div class="ad-field"><label for="logDate">Thời gian</label><input type="date" id="logDate" data-filter="logDate" value="${filters.logDate}"></div><div class="ad-field"><label for="logEmail">Tài khoản</label><select id="logEmail" data-filter="logEmail">${options([['','Tất cả tài khoản'],...new Set(state.activity.map(l=>l.email))],filters.logEmail)}</select></div><div class="ad-field"><label for="logAction">Thao tác</label><select id="logAction" data-filter="logAction">${options([['','Tất cả thao tác'],...new Set(state.activity.map(l=>l.action))],filters.logAction)}</select></div></div>${table(['Thời gian','Tài khoản','Thao tác','Đối tượng'],logs.map(l=>[esc(l.time)+'<br><small>'+dateText(l.date)+'</small>',esc(l.email),esc(l.action),esc(l.target)]))}<div class="ad-note">Chi tiết bản ghi: kết quả thao tác, nguồn truy cập và đối tượng liên quan theo dữ liệu backend. Hiện tại là nhật ký thao tác demo trên thiết bị.</div>`;
        }
    };
    function render() {root.innerHTML=renderers[page]?.()||'<div class="ad-card">Không tìm thấy màn hình.</div>';}
    function openDialog(title,body) {document.getElementById('adminDialogTitle').textContent=title;document.getElementById('adminDialogBody').innerHTML=body;document.getElementById('adminDialog').showModal();}
    function saveForm(form,event) {
        if(!form.reportValidity()) return;
        const d=Object.fromEntries(new FormData(form));Object.keys(d).forEach(k=>{if(typeof d[k]==='string')d[k]=d[k].trim();});
        switch(form.dataset.form) {
            case 'routes': {
                const old=get('routes');const id=old?.id||d.id.toUpperCase();
                if(!old&&state.routes.some(r=>r.id===id))return notify('Mã tuyến đã tồn tại.',true);
                const item={id,name:d.name,price:Number(d.price),stops:d.stops,status:old?.status||'Hoạt động'};
                if(!item.name||!item.stops)return notify('Vui lòng nhập tên tuyến và thứ tự trạm.',true);
                if(old)Object.assign(old,item);else state.routes.push(item);selected.routes=id;persist('Cập nhật tuyến',id);break;
            }
            case 'schedules': {
                const days=new FormData(form).getAll('days');if(!days.length)return notify('Chọn ít nhất một ngày áp dụng.',true);
                if(!filters.date)return notify('Chọn ngày chạy.',true);
                if(d.start===d.end)return notify('Giờ khởi hành và giờ đến không được trùng nhau.',true);
                if(state.schedules.some(s=>s.route===filters.route&&s.date===filters.date&&s.start===d.start&&s.id!==selected.schedules))return notify('Đã có chuyến vào giờ khởi hành này.',true);
                const old=get('schedules');let number=state.schedules.length+1;let id=old?.id||filters.route+'-'+String(number).padStart(2,'0');while(!old&&state.schedules.some(s=>s.id===id))id=filters.route+'-'+String(++number).padStart(2,'0');
                const item={id,route:filters.route,date:filters.date,start:d.start,end:d.end,days};if(old)Object.assign(old,item);else state.schedules.push(item);selected.schedules=id;persist('Cập nhật lịch trình',id);break;
            }
            case 'dispatch': {
                if(!d.vehicle||!d.driver||!d.assistant)return notify('Chọn đủ xe, tài xế và phụ xe.',true);
                const target=state.schedules.find(s=>s.id===d.trip);if(!target)return notify('Chọn chuyến hợp lệ.',true);
                const interval=s=>{const start=Number(s.start.slice(0,2))*60+Number(s.start.slice(3));let end=Number(s.end.slice(0,2))*60+Number(s.end.slice(3));if(end<=start)end+=1440;const midnight=Date.parse(s.date+'T00:00:00')/60000;return [midnight+start,midnight+end];};
                const [start,end]=interval(target);
                const conflict=state.dispatch.find(a=>{const s=state.schedules.find(x=>x.id===a.id);if(!s||a.id===d.trip)return false;const [otherStart,otherEnd]=interval(s);return start<otherEnd&&otherStart<end&&(a.vehicle===d.vehicle||a.driver===d.driver||a.assistant===d.assistant);});
                if(conflict)return notify('Trùng lịch xe hoặc nhân sự với chuyến '+conflict.id+'.',true);
                const item={id:d.trip,vehicle:d.vehicle,driver:d.driver,assistant:d.assistant};const old=state.dispatch.find(a=>a.id===d.trip);if(old)Object.assign(old,item);else state.dispatch.push(item);selected.dispatch=d.trip;persist('Phân công điều xe',d.trip);break;
            }
            case 'vouchers': {
                if(d.end<d.start)return notify('Ngày kết thúc phải từ ngày bắt đầu trở đi.',true);
                if(d.type==='percent'&&Number(d.value)>100)return notify('Giảm theo phần trăm không được vượt quá 100%.',true);
                const old=get('vouchers'),id=d.id.toUpperCase();if(state.vouchers.some(v=>v.id===id&&v!==old))return notify('Mã voucher đã tồn tại.',true);
                if(old&&Number(d.limit)<old.used)return notify('Giới hạn không được nhỏ hơn số lượt đã dùng.',true);
                const item={id,type:d.type,value:Number(d.value),minimum:Number(d.minimum),start:d.start,end:d.end,limit:Number(d.limit),used:old?.used||0};if(old)Object.assign(old,item);else state.vouchers.push(item);selected.vouchers=id;persist('Cập nhật voucher',id);break;
            }
            case 'approvals': {
                const decision=event.submitter?.value||'Đã duyệt';if(decision!=='Đã duyệt'&&!d.note)return notify('Nhập lý do bổ sung hoặc từ chối hồ sơ.',true);
                const item=get('approvals');item.status=decision;item.note=d.note;persist('Xét duyệt hồ sơ',item.id);break;
            }
            case 'support': {if(!d.reply)return notify('Nhập nội dung phản hồi.',true);const item=get('support');item.reply=d.reply;item.status=d.status;persist('Lưu phản hồi CSKH',item.id);break;}
            case 'accounts': {
                if(!d.name||!d.email)return notify('Nhập đầy đủ họ tên và email.',true);const old=get('accounts');if(state.accounts.some(a=>a.email.toLowerCase()===d.email.toLowerCase()&&a!==old))return notify('Email đã có trong danh sách mẫu.',true);
                const item={id:old?.id||'USER-'+Date.now(),name:d.name,email:d.email,role:d.role,status:old?.status||'Hoạt động'};if(old)Object.assign(old,item);else state.accounts.push(item);selected.accounts=item.id;persist('Cập nhật vai trò mẫu',item.id);break;
            }
            case 'station': {if(!d.name)return notify('Nhập tên trạm.',true);if(state.stops.includes(d.name))return notify('Trạm đã tồn tại.',true);state.stops.push(d.name);document.getElementById('adminDialog').close();persist('Thêm trạm',d.name);break;}
        }
    }
    document.addEventListener('submit',event=>{const form=event.target.closest('[data-form]');if(form){event.preventDefault();saveForm(form,event);}});
    root.addEventListener('change',event=>{const filter=event.target.dataset.filter;if(filter){if((filter==='from'&&event.target.value>filters.to)||(filter==='to'&&event.target.value<filters.from)){notify('Khoảng ngày không hợp lệ.',true);event.target.value=filters[filter];return;}filters[filter]=event.target.value;if(filter==='route'||filter==='date')selected.schedules=null;render();}});
    root.addEventListener('input',event=>{if(event.target.dataset.filter==='search'){const position=event.target.selectionStart;filters.search=event.target.value;render();const field=document.getElementById('routeSearch');field.focus();field.setSelectionRange(position,position);}});
    root.addEventListener('click',event=>{
        const row=event.target.closest('[data-select]');if(row){selected[row.dataset.select]=row.dataset.id;render();return;}
        const action=event.target.closest('[data-action]')?.dataset.action;
        if(action==='new-route'){selected.routes=null;render();document.getElementById('ad-id').focus();}
        if(action==='new-schedule'){selected.schedules=null;render();}
        if(action==='new-voucher'){selected.vouchers=null;render();document.getElementById('ad-id').focus();}
        if(action==='new-account'){selected.accounts=null;render();document.getElementById('ad-name').focus();}
        if(action==='new-stop')openDialog('Thêm trạm',`<form data-form="station">${input('name','Tên trạm','','text','required maxlength="100"','ad-station-name')}<div class="ad-actions">${primary('Lưu trạm mẫu','type="submit"')}</div></form>`);
        if(action==='attachment'){const item=get('approvals');openDialog('Tệp đính kèm · '+item.name,`<div class="ad-preview-sheet"><strong>HỒ SƠ MINH HỌA</strong>Họ tên: ${esc(item.name)}<br>Đối tượng: ${esc(item.category)}<br>${esc(item.school)}</div><p class="ad-details">Chưa có tài liệu cá nhân thật. Khi tích hợp backend, vị trí này sẽ hiển thị tệp do hành khách tải lên.</p>`);}
        if(action==='print')window.print();
        if(action==='export'){
            const tableElement=root.querySelector('table');
            const quote=value=>'"'+(/^[=+\-@]/.test(value)?"'":'')+value.replace(/"/g,'""')+'"';
            const rows=[['Báo cáo SmartBus Go','Dữ liệu minh họa'],['Từ ngày',filters.from,'Đến ngày',filters.to],...Array.from(tableElement.rows).map(r=>Array.from(r.cells).map(c=>c.textContent.trim()))];
            const blob=new Blob(['\uFEFF'+rows.map(r=>r.map(quote).join(',')).join('\r\n')],{type:'text/csv;charset=utf-8'});const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download='smartbus-bao-cao-demo.csv';link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);notify('Đã xuất báo cáo CSV, có thể mở bằng Excel.');
        }
    });
    document.querySelector('[data-close-dialog]').addEventListener('click',()=>document.getElementById('adminDialog').close());
    document.querySelector('.ad-menu').addEventListener('click',event=>{const open=document.body.classList.toggle('ad-menu-open');event.currentTarget.setAttribute('aria-expanded',String(open));});
    document.addEventListener('keydown',event=>{if(event.key==='Escape'){document.body.classList.remove('ad-menu-open');document.querySelector('.ad-menu').setAttribute('aria-expanded','false');}});
    render();
    if(!storageAvailable)notify('Không đọc được dữ liệu đã lưu. Đang dùng bộ dữ liệu minh họa.',true);
})();

