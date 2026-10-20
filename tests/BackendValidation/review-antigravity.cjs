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
})();
