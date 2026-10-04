"""HTTP integration checks for the disposable Development demo. No external packages.
Run against a fresh demo process: python tools/check_demo.py http://localhost:5188
"""
import concurrent.futures
import datetime as dt
import html
import http.cookiejar
import json
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5188'
tomorrow = (dt.datetime.now(dt.timezone(dt.timedelta(hours=7))).date() + dt.timedelta(days=1)).isoformat()
checks = 0

def check(condition, message):
    global checks
    assert condition, message
    checks += 1
    print('PASS', message)

class Session:
    def __init__(self):
        self.client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def request(self, path, fields=None, payload=None, token=None):
        headers = {}
        data = None
        if fields is not None:
            data = urllib.parse.urlencode(fields).encode()
        if payload is not None:
            data = json.dumps(payload).encode()
            headers['Content-Type'] = 'application/json'
        if token: headers['RequestVerificationToken'] = token
        try:
            response = self.client.open(urllib.request.Request(BASE + path, data, headers), timeout=20)
        except urllib.error.HTTPError as error:
            response = error
        return response.status, html.unescape(response.read().decode()), response.url
    def token(self, path):
        status, text, _ = self.request(path)
        assert status == 200, (path, status, text[:200])
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', text)
        assert match, path
        return match[1]
    def post(self, path, fields, source):
        return self.request(path, {'__RequestVerificationToken': self.token(source), **fields})
    def login(self, driver=False):
        path = '/Driver/Login' if driver else '/Account/Login'
        fields = {'DriverIdentifier':'TX-001'} if driver else {'Username':'demo@smartbus.vn'}
        return self.post(path, {**fields, 'Password':'Demo123!'}, path)

s = Session()
check(s.request('/')[0] == 200, 'home renders')
search = '/Trip/Search?' + urllib.parse.urlencode({'from':'Hà Nội','to':'Hải Phòng','date':tomorrow})
status, text, _ = s.request(search)
check(status == 200 and '3 chuyến xe' in text, 'three sample trips render')
check('asp-route-' not in text, 'Razor tag helpers render links')
check(s.request('/Ticket/Detail?id=SBG-DEMO-001-A01')[2].find('/Account/Login') > 0, 'anonymous ticket access requires login')
_, text, _ = s.post('/Account/Login', {'Username':'demo@smartbus.vn','Password':'bad'}, '/Account/Login')
check('Sai' in text or 'không' in text, 'incorrect customer password rejected')
check(s.login()[0] == 200, 'customer login succeeds')
check('BK-DEMO-001' in s.request('/Ticket/MyTickets')[1], 'seed bookings listed')
check('A01' in s.request('/Ticket/Detail?id=SBG-DEMO-001-A01')[1], 'owned ticket renders')
token = s.token(search)
payload = {'tripId':11,'date':tomorrow,'seats':['D07']}
check(s.request('/Ticket/Hold', payload=payload)[0] == 400, 'CSRF missing rejected')
check(s.request('/Ticket/Hold', payload={**payload,'seats':['A01']},token=token)[0] == 409, 'booked seat rejected')
check(s.request('/Ticket/Hold', payload={**payload,'seats':[None]},token=token)[0] == 409, 'null seat rejected without server crash')
check(s.request('/Ticket/Hold', payload={**payload,'seats':['A01','B01','C01','D01','A02','B02','C02']},token=token)[0] == 409, 'more than six seats rejected')

other = Session()
other.request('/Account/ExternalLogin?provider=Google')
other_token = other.token(search)
with concurrent.futures.ThreadPoolExecutor(2) as pool:
    results = list(pool.map(lambda item:item[0].request('/Ticket/Hold',payload=payload,token=item[1]), [(s,token),(other,other_token)]))
check(sorted(r[0] for r in results) == [200,409], 'concurrent same-seat reservations have one winner')
winner = s if results[0][0] == 200 else other
loser = other if winner is s else s
code = json.loads(next(r[1] for r in results if r[0] == 200))['data']['bookingCode']
payment = '/Ticket/Payment?code=' + code
check(loser.request(payment)[0] == 404, 'other customer cannot read payment')
expiry = re.search(r'data-expires="([^"]+)"', winner.request(payment)[1])[1]
check(expiry == re.search(r'data-expires="([^"]+)"', winner.request(payment)[1])[1], 'reload preserves hold deadline')
check('thất bại' in winner.post('/Ticket/SimulateFailure',{'code':code},payment)[1], 'simulated failure does not issue ticket')
check('không hợp lệ' in winner.post('/Ticket/ConfirmPayment',{'code':code,'method':'VietQR','voucher':'INVALID'},payment)[1], 'invalid voucher rejected by server')
_, text, url = winner.post('/Ticket/ConfirmPayment',{'code':code,'method':'MoMo','voucher':'SMARTBUSNEW'},payment)
check('/Ticket/Detail' in url and '130,000' in text, 'payment issues discounted ticket')
ticket = urllib.parse.parse_qs(urllib.parse.urlsplit(url).query).get('id', [urllib.parse.urlsplit(url).path.rsplit('/',1)[-1]])[0]
check(loser.request('/Ticket/Detail?id=' + ticket)[0] == 404, 'other customer cannot read ticket')
check('/Ticket/Detail' in winner.post('/Ticket/ConfirmPayment',{'code':code,'method':'MoMo'},payment)[2], 'repeat payment is idempotent')
manage = '/Ticket/Manage?code=' + code
check('Đổi chuyến thành công' in winner.post('/Ticket/Change',{'code':code,'tripId':13,'date':tomorrow},manage)[1], 'change to same-price trip succeeds')
check('hủy vé' in winner.post('/Ticket/Cancel',{'code':code},manage)[1], 'cancel succeeds')
check('Đã hủy vé' in winner.request('/Ticket/Detail?id='+ticket)[1], 'cancelled ticket shows cancelled state')
check(s.request('/Driver/Dashboard')[2].find('/Account/AccessDenied') > 0 or s.request('/Driver/Dashboard')[0] == 403, 'customer cannot access driver dashboard')
driver = Session()
_, text, _ = driver.post('/Driver/Login',{'DriverIdentifier':'TX-001','Password':'bad'},'/Driver/Login')
check('Tài khoản mẫu' in text, 'incorrect driver password rejected')
check(driver.login(True)[0] == 200, 'driver login succeeds')
fields = {'ticket':'SBG-DEMO-001-A01','tripId':11,'date':tomorrow}
check('không thuộc chuyến' in driver.post('/Driver/CheckIn',{**fields,'tripId':13},'/Driver/Dashboard')[1], 'wrong-trip check-in rejected')
check('Vé hợp lệ' in driver.post('/Driver/CheckIn',fields,'/Driver/Dashboard')[1], 'check-in succeeds')
check('trước đó' in driver.post('/Driver/CheckIn',fields,'/Driver/Dashboard')[1], 'duplicate check-in rejected')
check('bị hủy' in driver.post('/Driver/CheckIn',{'ticket':ticket,'tripId':13,'date':tomorrow},'/Driver/Dashboard')[1], 'cancelled ticket check-in rejected')
check('sự cố kiểm thử' in driver.post('/Driver/Incident',{'message':'sự cố kiểm thử'},'/Driver/Dashboard')[1], 'driver incident saved')
profile = {'FullName':'Nguyễn Văn An Demo','Email':'demo@smartbus.vn','Phone':'0987654321','BirthDate':'2004-05-12','Address':'Thái Nguyên','DiscountGroup':'Thông thường'}
check('không hợp lệ' in s.post('/Customer/Profile',{**profile,'Phone':'123'},'/Customer/Profile')[1], 'invalid profile phone rejected')
check('Đã lưu hồ sơ' in s.post('/Customer/Profile',profile,'/Customer/Profile')[1], 'profile saved')
check('Chờ xét duyệt' in s.post('/Customer/RequestDiscount',{'group':'Sinh viên'},'/Customer/Profile')[1], 'discount request pending')
check('Chỉ hủy vé chưa sử dụng' in s.post('/Ticket/Cancel',{'code':'BK-DEMO-001'},'/Ticket/Manage?code=BK-DEMO-001')[1], 'checked-in booking cannot cancel')
new = Session()
new_email = 'test-' + dt.datetime.now().strftime('%H%M%S') + '@smartbus.vn'
_, text, url = new.post('/Account/Register',{'FullName':'Khách Demo','Email':new_email,'Phone':'0971234567','Password':'Demo123!','ConfirmPassword':'Demo123!','AcceptTerms':'true'},'/Account/Register')
check('/Account/VerifyOtp' in url and '123456' in text, 'registration displays demo OTP')
check('/Account/Login' in new.post('/Account/VerifyOtp',{'Email':new_email,'OtpCode':'123456'},urllib.parse.urlsplit(url).path+'?'+urllib.parse.urlsplit(url).query)[2], 'OTP activates account')
check('Khách Demo' in new.post('/Account/Login',{'Username':new_email,'Password':'Demo123!'},'/Account/Login')[1], 'new account login succeeds')
print(f'All {checks} integration checks passed.')
