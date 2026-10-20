// Run baseline checks by default; --workflow exercises release/reload/expiry/races.
// The workflow gate intentionally fails candidates that have not closed those bugs.
if (process.argv.includes('--workflow')) {
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
 const ctx={tripBookingData:{},tripMutationGen:{}, window:{sbgCurrentUserId:2}, getOrCreateSessionId:()=> 'test',showSeatAlert(){}, console:{error(){}},
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
