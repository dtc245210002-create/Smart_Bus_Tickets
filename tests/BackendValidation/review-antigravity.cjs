// Run baseline checks by default; --workflow exercises release/reload/expiry/races.
// The workflow gate intentionally fails candidates that have not closed those bugs.
const reviewPath = require('path').resolve(__dirname, '../..');
const selectedSource = require('child_process').execFileSync('git', ['show', `${process.argv[2] || 'HEAD'}:wwwroot/js/seat-selection.js`], {cwd:reviewPath,encoding:'utf8'});
if (process.argv.includes('--browser-smoke') || process.argv.includes('--expiry-watch')) {
// Installed Edge + a fresh profile avoids downloading a Playwright driver.
// This talks only to the disposable fixture and this process's browser instance.
const fs=require('fs'),path=require('path'),cp=require('child_process');
const root=reviewPath;
const fixture=JSON.parse(fs.readFileSync(path.join(root,'TestResults/browser-fixture/context.json'),'utf8'));
if(fixture.status!=='READY'||!/^http:\/\/127\.0\.0\.1:\d+$/.test(fixture.baseUrl)||new Date(fixture.expiresAtUtc)<=new Date()) throw Error('A ready, unexpired local disposable fixture is required');
const exe='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
if(!fs.existsSync(exe))throw Error('Installed Edge unavailable; no driver download attempted');
const dir=path.join(root,'TestResults/browser-smoke',`${Date.now()}`);fs.mkdirSync(dir,{recursive:true});
const profile=path.join(dir,'profile'),checks=[],errors=[],pages=[],holdPosts=[];
const password=fs.readFileSync(path.join(root,'tests/BackendValidation/TestSupport/TestDatabase.cs'),'utf8').match(/PasswordHasher\.HashPassword\("([^"]+)"\)/)?.[1];
if(!password)throw Error('Synthetic fixture password not found');
const edge=cp.spawn(exe,['--headless=new','--remote-debugging-address=127.0.0.1','--remote-debugging-port=0',`--user-data-dir=${profile}`,'--no-first-run','--no-default-browser-check','--disable-extensions','--disable-sync','about:blank'],{windowsHide:true,stdio:'ignore'});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));let ws,sequence=0;const pending=new Map();
function record(name,passed,observed){checks.push({name,passed:!!passed,observed});console.log(`${passed?'PASS':'FAIL'} ${name}`);}
function command(method,params={},sessionId){return new Promise((resolve,reject)=>{const id=++sequence;const timer=setTimeout(()=>{pending.delete(id);reject(Error(`CDP timeout: ${method}`))},15000);pending.set(id,{resolve,reject,timer});ws.send(JSON.stringify({id,method,params,...(sessionId?{sessionId}:{})}));});}
async function page(){const c=await command('Target.createBrowserContext');const t=await command('Target.createTarget',{url:'about:blank',browserContextId:c.browserContextId});const a=await command('Target.attachToTarget',{targetId:t.targetId,flatten:true});const session=a.sessionId;pages.push(session);await command('Page.enable',{},session);await command('Runtime.enable',{},session);return session;}
async function evaluate(session,expression){const r=await command('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},session);if(r.exceptionDetails)throw Error(r.exceptionDetails.text+': '+(r.exceptionDetails.exception?.description||''));return r.result.value;}
async function until(session,expression){for(let i=0;i<60;i++){try{if(await evaluate(session,expression))return;}catch{}await sleep(250);}throw Error('Browser condition timed out: '+expression);}
async function navigate(session,url){const marker=String(Date.now())+Math.random();await evaluate(session,`window.__qaNavMarker=${JSON.stringify(marker)}`);await command('Page.navigate',{url},session);const pathname=new URL(url).pathname;await until(session,`window.__qaNavMarker!==${JSON.stringify(marker)} && location.pathname===${JSON.stringify(pathname)} && document.readyState!=='loading'`);}
async function click(session,selector){const r=await evaluate(session,`(()=>{const e=document.querySelector(${JSON.stringify(selector)});if(!e)throw Error('Element missing');e.scrollIntoView({block:'center',behavior:'instant'});const r=e.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2}})()`);await command('Input.dispatchMouseEvent',{type:'mousePressed',button:'left',clickCount:1,...r},session);await command('Input.dispatchMouseEvent',{type:'mouseReleased',button:'left',clickCount:1,...r},session);}
async function login(session,email){await navigate(session,fixture.baseUrl+'/Account/Login');await evaluate(session,`(()=>{document.querySelector('[name="Username"]').value=${JSON.stringify(email)};document.querySelector('[name="Password"]').value=${JSON.stringify(password)};document.querySelector('[name="Username"]').form.requestSubmit();return true})()`);await until(session,"location.pathname!='/Account/Login' && document.readyState!=='loading'");}
async function capture(session,name){const shot=await command('Page.captureScreenshot',{format:'png',captureBeyondViewport:false},session);fs.writeFileSync(path.join(dir,name+'.png'),Buffer.from(shot.data,'base64'));}
const trip=fixture.fixture.Trip;
const search=fixture.baseUrl+'/Trip/Search?from='+encodeURIComponent('Hà Nội')+'&to='+encodeURIComponent('Hải Phòng')+'&date='+fixture.fixture.ServiceDate;
const accordion='#seatAccordion-'+trip;
const seat=code=>`${accordion} .sbg-seat-box[data-code="${code}"]`;
const checkout=accordion+' .btn-continue-booking';
async function openMap(session){await navigate(session,search);await until(session,`!!document.querySelector('.btn-toggle-seat-map[data-trip-id="${trip}"]')`);await click(session,`.btn-toggle-seat-map[data-trip-id="${trip}"]`);await until(session,`document.querySelector(${JSON.stringify(accordion)}).classList.contains('show')`);}
(async()=>{
 try{
  let activePort;for(let i=0;i<80;i++){if(fs.existsSync(path.join(profile,'DevToolsActivePort'))){activePort=fs.readFileSync(path.join(profile,'DevToolsActivePort'),'utf8').trim().split(/\r?\n/);break;}await sleep(250);}
  if(!activePort)throw Error('Installed headless browser did not expose its test endpoint');
  ws=new WebSocket(`ws://127.0.0.1:${activePort[0]}${activePort[1]}`);
  await new Promise((resolve,reject)=>{ws.addEventListener('open',resolve,{once:true});ws.addEventListener('error',reject,{once:true});});
  ws.addEventListener('message',event=>{const m=JSON.parse(event.data);if(m.method==='Runtime.exceptionThrown')errors.push({browserException:m.params.exceptionDetails.exception?.description||m.params.exceptionDetails.text});if(m.method==='Network.requestWillBeSent'&&m.params.request.method==='POST'&&m.params.request.url.includes('/api/bookings/hold-seats'))holdPosts.push(m.params.request.url);if(m.id&&pending.has(m.id)){const p=pending.get(m.id);pending.delete(m.id);clearTimeout(p.timer);m.error?p.reject(Error(m.error.message)):p.resolve(m.result);}});
  const a=await page(),b=await page();
  await command('Network.enable',{},a);
  await command('Emulation.setDeviceMetricsOverride',{width:1440,height:1000,deviceScaleFactor:1,mobile:false},a);
  await login(a,'g0.customer.a@example.invalid');record('Desktop customer logs in through actual HTML form',true);
  await openMap(a);await until(a,`document.querySelector(${JSON.stringify(seat('A01'))}).classList.contains('selected')`);
  if(process.argv.includes('--expiry-watch')){
   const initial=await evaluate(a,`fetch('/api/bookings/trip-seat-status?tripId=${trip}').then(r=>r.json())`);
   record('Fresh server hold has original positive ten-minute TTL',initial.heldBookingCode==='G0-PENDING'&&initial.remainingSeconds>540&&initial.remainingSeconds<=600,initial);
   if(!checks.at(-1).passed)throw Error('Expiry watch requires a fresh fixture; do not age or renew the booking');
   const started=Date.now(),deadline=started+(initial.remainingSeconds+12)*1000;
   console.log(`EXPIRY_WATCH_RUNNING resultDir=${dir}; initialTTL=${initial.remainingSeconds}`);
   await capture(a,'expiry-before');
   while(Date.now()<deadline){
    const state=await evaluate(a,`({selected:document.querySelector(${JSON.stringify(seat('A01'))}).classList.contains('selected'),disabled:document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')})`);
    fs.writeFileSync(path.join(dir,'progress.json'),JSON.stringify({status:'WAITING_REAL_TTL',elapsedSeconds:Math.floor((Date.now()-started)/1000),initialTtl:initial.remainingSeconds,fixtureRunId:fixture.runId},null,2));
    if(!state.selected&&state.disabled)break;
    await sleep(1000);
   }
   const final=await evaluate(a,`fetch('/api/bookings/trip-seat-status?tripId=${trip}').then(r=>r.json())`);
   const expiredUi=await evaluate(a,`!document.querySelector(${JSON.stringify(seat('A01'))}).classList.contains('selected') && document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
   record('Real wall-clock TTL expires server quote and browser selection',expiredUi&&final.heldBookingCode===null&&final.remainingSeconds===0&&final.myHeldSeats.length===0&&Date.now()-started>=(initial.remainingSeconds-5)*1000,{elapsedSeconds:Math.floor((Date.now()-started)/1000),final});
   record('Polling did not issue background hold POSTs or renew TTL',holdPosts.length===0,{holdPosts:holdPosts.length});
   await capture(a,'expiry-after');
   await login(b,'g0.customer.b@example.invalid');await openMap(b);await click(b,seat('A01'));
   await until(b,`document.querySelector(${JSON.stringify(seat('A01'))}).classList.contains('selected') && !document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
   const reclaimed=await evaluate(b,`fetch('/api/bookings/trip-seat-status?tripId=${trip}').then(r=>r.json())`);
   record('Another browser customer can reclaim the expired seat',reclaimed.heldBookingCode&&reclaimed.heldBookingCode!=='G0-PENDING'&&reclaimed.myHeldSeats.includes('A01')&&reclaimed.remainingSeconds>0,reclaimed);
   fs.writeFileSync(path.join(dir,'progress.json'),JSON.stringify({status:'COMPLETED',fixtureRunId:fixture.runId}));
   return;
  }
  await click(a,seat('A02'));await until(a,`document.querySelector(${JSON.stringify(seat('A02'))}).classList.contains('selected') && !document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  const bookingUrl=await evaluate(a,`document.querySelector(${JSON.stringify(checkout)}).href`);
  record('Real seat click reaches owned booking Payment URL',new URL(bookingUrl).searchParams.get('bookingCode')==='G0-PENDING',bookingUrl);
  await capture(a,'desktop-held');
  await login(b,'g0.customer.b@example.invalid');await openMap(b);
  await until(b,`document.querySelector(${JSON.stringify(seat('A02'))}).classList.contains('booked')`);
  record('Second isolated customer sees the first customer seat blocked',true);
  await command('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false},a);
  await openMap(a);await until(a,`!document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  record('Mobile-width reload recovers actual same booking',await evaluate(a,`document.querySelector(${JSON.stringify(checkout)}).href===${JSON.stringify(bookingUrl)}`));
  await click(a,seat('A03'));await until(a,`document.querySelector(${JSON.stringify(seat('A03'))}).classList.contains('selected') && !document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  await click(a,seat('A02'));await until(a,`!document.querySelector(${JSON.stringify(seat('A02'))}).classList.contains('selected') && !document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  const state=await evaluate(a,`(async()=>{const q=await fetch('/api/bookings/trip-seat-status?tripId=${trip}').then(r=>r.json());return {q,total:document.querySelector(${JSON.stringify(accordion+' .selected-total-price')}).innerText,href:document.querySelector(${JSON.stringify(checkout)}).href}})()`);
  record('Browser release recovers server quote for the exact remaining seats',state.q.myHeldSeats.includes('A03')&&!state.q.myHeldSeats.includes('A02')&&state.total.replace(/\D/g,'')===String(state.q.heldTotalPrice),state);
  await capture(a,'mobile-after-release');
  await command('Network.enable',{},a);
  await command('Network.emulateNetworkConditions',{offline:true,latency:0,downloadThroughput:0,uploadThroughput:0},a);
  await until(a,`document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  record('Real browser offline polling invalidates checkout quote',true);
  await command('Network.emulateNetworkConditions',{offline:false,latency:0,downloadThroughput:-1,uploadThroughput:-1},a);
  await until(a,`!document.querySelector(${JSON.stringify(checkout)}).classList.contains('disabled')`);
  record('Network restore recovers same booking without re-holding',await evaluate(a,`document.querySelector(${JSON.stringify(checkout)}).href===${JSON.stringify(bookingUrl)}`));
  await click(a,checkout);await until(a,"location.pathname==='/Ticket/Payment' && !!document.getElementById('paymentConfirmForm')");
  await click(a,'#btnConfirmPayment');await until(a,"!!document.getElementById('paymentStatusAlert') && document.getElementById('paymentStatusAlert').innerText.includes('503') && !document.getElementById('btnConfirmPayment').disabled");
  record('Actual Payment503 stays on page and permits retry',await evaluate(a,"location.pathname==='/Ticket/Payment'"));
  await capture(a,'mobile-payment503');
  record('Mobile viewport is 390 pixels',await evaluate(a,'innerWidth')===390,await evaluate(a,'innerWidth'));
 }catch(e){errors.push({name:e.name,message:e.message});record('Browser smoke completed',false,e.message);for(let i=0;i<pages.length;i++){try{await capture(pages[i],'failure-'+i);errors.push({page:i,diagnostic:await evaluate(pages[i],"({url:location.href,ready:document.readyState,body:document.body.innerText.slice(0,1400),scripts:Array.from(document.scripts).map(s=>s.src)})")});}catch{}}}
 finally{
  if(ws?.readyState===WebSocket.OPEN){try{await command('Browser.close');}catch{}ws.close();}
  if(edge.exitCode===null)edge.kill();
  const result={sourceCommit:cp.execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),method:'Installed headless Edge, isolated fresh contexts, actual DOM/input and HTTP; not physical mobile',environment:fixture.environment,fixtureRunId:fixture.runId,checks,errors,artifacts:dir,notVerified:['physical mobile','real payment provider','long offline and full expiry browser matrix','driver/admin browser flows in this runner']};
  fs.writeFileSync(path.join(dir,'result.json'),JSON.stringify(result,null,2));
  console.log(`${checks.filter(c=>c.passed).length}/${checks.length}; result=${path.join(dir,'result.json')}`);
  if(checks.some(c=>!c.passed))process.exitCode=1;
 }
})().catch(e=>{console.error(e.message);process.exitCode=1});
} else if (process.argv.includes('--workflow') || selectedSource.includes('const tripMutationGen =')) {
const cp = require('child_process'), vm = require('vm'), fs = require('fs');
const path = require('path');
const repo = path.resolve(__dirname, '../..');
const commit = process.argv[2] || 'HEAD';
const blob = file => cp.execFileSync('git', ['show', `${commit}:${file}`], {cwd:repo,encoding:'utf8'});
const source = blob('wwwroot/js/seat-selection.js');
const fragment = source.slice(source.indexOf('    async function toggleSeatHold('), source.indexOf('    // Lắng nghe sự kiện thay đổi trạm'));
const checks = [];
function check(name, passed, observed) { checks.push({name, passed:!!passed, observed}); }
function element(code, selected=false) {
 const classes = new Set(selected ? ['selected'] : []);
 return {style:{}, innerText:'', innerHTML:'', attrs:{'data-code':code, 'data-price':'100000'},
 classList:{add(...cs){cs.forEach(c=>classes.add(c))}, remove(...cs){cs.forEach(c=>classes.delete(c))}, contains:c=>classes.has(c)},
 getAttribute(k){return this.attrs[k]}, setAttribute(k,v){this.attrs[k]=v}};
}
function setup() {
 const seats = [element('A02',true),element('A03',true)], btn=element(), total=element(), list=element();
 const accordion = element(); accordion.classList.add('show');
 accordion.querySelectorAll=s=> s.endsWith('.selected') ? seats.filter(x=>x.classList.contains('selected')) : seats;
 accordion.querySelector=s=> ({'.selected-seats-list':list,'.selected-total-price':total,'.btn-continue-booking':btn})[s];
 const store = new Map();
 const ctx={tripBookingData:{},tripMutationGen:{},inFlightMutations:{}, window:{sbgCurrentUserId:2}, getOrCreateSessionId:()=> 'test',showSeatAlert(){}, console:{error(){}},
 document:{getElementById:()=>accordion}, sessionStorage:{setItem:(k,v)=>store.set(k,v),getItem:k=>store.get(k),removeItem:k=>store.delete(k)},
 fetch:async()=>({ok:true,status:200,json:async()=>({success:true})})};
 vm.createContext(ctx); vm.runInContext(fragment,ctx);
 const poll=ctx.syncSeatStatuses; ctx.syncSeatStatuses=()=>{};
 return {ctx,seats,btn,total,list,store,poll};
}
function response(data,status=200){return {ok:status>=200&&status<300,status,json:async()=>data};}
const quote={bookingCode:'BK-real',totalPrice:200000,heldSeats:['A02','A03'],remainingSeconds:540};
(async()=>{
 for(const status of [400,401,403,409,503]) {
  const s=setup(); s.seats[0].classList.remove('selected');
  s.ctx.fetch=async()=>response({success:false},status);
  await s.ctx.toggleSeatHold('1','A02',true,s.seats[0]);
  check(`${status} hold classification`,s.seats[0].classList.contains('booked')===(status===409)&&!s.seats[0].classList.contains('selected'));
 }
 {
  const s=setup(); s.ctx.tripBookingData['1']={...quote};
  await s.ctx.toggleSeatHold('1','A02',false,s.seats[0]);
  check('Release invalidates old total and disables checkout', s.ctx.tripBookingData['1'].totalPrice===undefined&&s.btn.classList.contains('disabled'));
  s.ctx.fetch=async()=>response({success:true,bookedSeats:[],myHeldSeats:['A03'],heldBookingCode:'BK-real',heldTotalPrice:100000,remainingSeconds:500});
  await s.poll('1');
  check('Agreed004 response recovers checkout after release',!s.btn.classList.contains('disabled')&&s.ctx.tripBookingData['1']?.totalPrice===100000,s.btn.innerHTML);
 }
 {
  const s=setup(); s.store.set('sbg_hold_1',JSON.stringify({...quote,totalPrice:1}));
  s.ctx.fetch=async()=>response({success:true,bookedSeats:[],myHeldSeats:['A02','A03']});
  await s.poll('1');
  check('Unverified sessionStorage price does not enable checkout',s.btn.classList.contains('disabled'),s.btn.innerHTML);
 }
 {
  const s=setup(); s.ctx.fetch=async()=>response({success:true,bookedSeats:[],myHeldSeats:['A02','A03'],heldBookingCode:'BK-real',heldTotalPrice:200000,remainingSeconds:0});
  await s.poll('1');
  check('Zero TTL disables checkout and is not replaced by 600',s.btn.classList.contains('disabled')&&s.ctx.tripBookingData['1']?.remainingSeconds!==600,s.ctx.tripBookingData['1']);
 }
 {
  const s=setup(); let resolve; s.ctx.fetch=()=>new Promise(r=>resolve=r);
  const poll=s.poll('1');
  s.ctx.fetch=async()=>response({success:true,data:{holdId:'BK-new',totalPrice:200000,heldSeats:['A02','A03'],remainingSeconds:500}});
  await s.ctx.toggleSeatHold('1','A03',true,s.seats[1]);
  resolve(response({success:true,bookedSeats:[],myHeldSeats:['A02']})); await poll;
  check('Poll started before hold cannot erase confirmed selection',s.seats[1].classList.contains('selected'));
 }
 {
  const s=setup(); let finishHold,finishPoll;
  s.ctx.fetch=()=>new Promise(r=>finishHold=r);
  const hold=s.ctx.toggleSeatHold('1','A03',true,s.seats[1]);
  s.ctx.fetch=()=>new Promise(r=>finishPoll=r);
  const poll=s.poll('1');
  finishHold(response({success:true,data:{holdId:'BK-new',totalPrice:200000,heldSeats:['A02','A03'],remainingSeconds:500}}));await hold;
  finishPoll(response({success:true,bookedSeats:[],myHeldSeats:['A02'],heldBookingCode:'BK-new',heldTotalPrice:100000,remainingSeconds:499}));await poll;
  check('Poll begun during hold cannot overwrite its completed result',s.seats[1].classList.contains('selected')&&s.ctx.tripBookingData['1']?.totalPrice===200000,s.ctx.tripBookingData['1']);
 }
 {
  const s=setup(); s.ctx.tripBookingData['1']={...quote}; s.ctx.updateSeatSummary('1');
  s.ctx.fetch=async()=>response({success:false},503);await s.poll('1');
  check('Failed poll invalidates checkout quote',s.btn.classList.contains('disabled'),s.btn.innerHTML);
 }
 {
  const s=setup(); s.ctx.tripBookingData['1']={...quote};
  s.ctx.syncSeatStatuses=s.poll; let reads=0;
  s.ctx.fetch=async url=> {
   if (url.includes('release-seats')) return response({success:true});
   reads++; return response({success:true,bookedSeats:[],myHeldSeats:['A03'],heldBookingCode:'BK-real',heldTotalPrice:100000,remainingSeconds:500});
  };
  await s.ctx.toggleSeatHold('1','A02',false,s.seats[0]);
  // Drain the actual unawaited automatic GET; do not manually invoke another poll.
  for(let i=0;i<8;i++) await Promise.resolve();
  check('Automatic snapshot after release restores one-seat quote without waiting for interval',reads===1&&!s.btn.classList.contains('disabled')&&s.ctx.tripBookingData['1']?.totalPrice===100000,{reads,button:s.btn.innerHTML,quote:s.ctx.tripBookingData['1']});
 }
 {
  const payment=blob('Views/Ticket/Payment.cshtml');
  const body=payment.slice(payment.indexOf('            const confirmForm ='),payment.lastIndexOf('        });'));
  let submit, resolve, requests=0; const button=element(), text=element(), alert=element(); const location={href:'unchanged'};
  const ctx={window:{location},document:{getElementById:id=>id==='paymentConfirmForm'?{action:'/Ticket/ConfirmPayment',addEventListener:(_,cb)=>submit=cb}:id==='btnConfirmPayment'?button:id==='btnConfirmText'?text:alert},
  FormData:class{}, console:{error(){}},fetch:()=>{requests++;return new Promise(r=>resolve=r)}};
  vm.runInNewContext(body,ctx); const event={preventDefault(){},stopImmediatePropagation(){}};
  const pending=submit(event); await submit(event);
  check('Repeated payment submit sends one POST',requests===1);
  resolve(response({},503)); await pending;
  check('Payment503 stays on page and unlocks retry',!button.disabled&&location.href==='unchanged'&&!alert.classList.contains('hidden'));
 }
 const result={commit,method:'Node vm executing actual functions; mocked DOM/fetch; not browser E2E',checks};
 fs.mkdirSync(path.join(repo,'TestResults/frontend-review'),{recursive:true});
 fs.writeFileSync(path.join(repo,`TestResults/frontend-review/${commit.slice(0,7)}-behavior-review.json`),JSON.stringify(result,null,2));
 checks.forEach(c=>console.log(`${c.passed?'PASS':'FAIL'} ${c.name}${c.passed?'':': '+JSON.stringify(c.observed)}`));
 console.log(`${checks.filter(c=>c.passed).length}/${checks.length}`);
 if (checks.some(c=>!c.passed)) process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1});

} else {
const fs = require('fs');
const vm = require('vm');
const cp = require('child_process');
const path = require('path');
const repo = path.resolve(__dirname, '../..');
const commit = process.argv[2] || 'HEAD';
const source = cp.execFileSync('git', ['show', `${commit}:wwwroot/js/seat-selection.js`], { cwd: repo, encoding: 'utf8' });
const start = source.indexOf('    async function toggleSeatHold(');
const end = source.indexOf('    // 3. Cơ chế', start);
if (start < 0 || end < 0) throw Error('Cannot locate reviewed function');
const fn = source.slice(start, end);
const checks = [];
async function test(name, status, data, selecting, expectedBooked, expectedSelected, network = false) {
  const classes = new Set(selecting ? [] : ['selected']);
  const seat = { style: {}, classList: { add: c => classes.add(c), remove: c => classes.delete(c), contains: c => classes.has(c) } };
  const context = { window: { sbgCurrentUserId: 2 }, getOrCreateSessionId: () => 'test',
    showSeatAlert: () => {}, updateSeatSummary: () => {}, console: { error: () => {} },
    fetch: async () => { if (network) throw Error('offline'); return { ok: status >= 200 && status < 300, status,
      json: async () => { if (data === null) throw Error('not JSON'); return data; } }; } };
  vm.createContext(context); vm.runInContext(fn, context);
  await context.toggleSeatHold('1', 'A02', selecting, seat);
  checks.push({ name, passed: classes.has('booked') === expectedBooked && classes.has('selected') === expectedSelected,
    observed: { booked: classes.has('booked'), selected: classes.has('selected'), pointerEvents: seat.style.pointerEvents } });
}
(async () => {
  await test('Hold success follows server', 200, { success: true, data: { remainingSeconds: 600 } }, true, false, true);
  await test('409 marks conflicting seat', 409, { success: false }, true, true, false);
  for (const status of [400, 401, 403, 503])
    await test(`${status} must not mark seat sold`, status, { success: false }, true, false, false);
  await test('Non-JSON must not mark seat sold', 200, null, true, false, false);
  await test('Network failure does not select', 0, null, true, false, false, true);
  await test('Failed release retains selection', 503, { success: false }, false, false, true);
  await test('Confirmed release removes selection', 200, { success: true }, false, false, false);
  const payment = cp.execFileSync('git', ['show', `${commit}:Views/Ticket/Payment.cshtml`], { cwd: repo, encoding: 'utf8' });
  const a = payment.indexOf('            const confirmForm =');
  const b = payment.lastIndexOf('        });');
  let submit;
  const button = { disabled: false, classList: { add() {} } };
  const context = { document: { getElementById: id => id === 'paymentConfirmForm' ? { addEventListener: (_, cb) => { submit = cb; } }
    : id === 'btnConfirmPayment' ? button : { innerHTML: '' } } };
  vm.runInNewContext(payment.slice(a, b), context);
  submit({ preventDefault() {} }); let prevented = false;
  submit({ preventDefault() { prevented = true; }, stopImmediatePropagation() {} });
  checks.push({ name: 'Repeated submit event must prevent navigation/request', passed: prevented, observed: { disabled: button.disabled, prevented } });
  fs.mkdirSync(path.join(repo, 'TestResults/frontend-review'), { recursive: true });
  fs.writeFileSync(path.join(repo, `TestResults/frontend-review/${commit.slice(0,7)}-review.json`), JSON.stringify({ commit, method: 'Node vm with mocked fetch/DOM; not browser E2E', checks }, null, 2));
  for (const check of checks) console.log(`${check.passed ? 'PASS' : 'FAIL'} ${check.name}`);
  console.log(`${checks.filter(c => c.passed).length}/${checks.length} expected behaviors met`);
  if (checks.some(c => !c.passed)) process.exitCode = 1;
})();

}
