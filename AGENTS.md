# SMART Bus Go — quy tắc phối hợp Codex / Antigravity

RULES_VERSION: SMARTBUS-RULES-001

## Nguồn chung và ưu tiên

Đọc file này khi bắt đầu, tiếp tục sau khi dừng, trước mỗi nhóm sửa,
trước commit và bàn giao. Đọc lại cả ba file trao đổi tại mỗi mốc đó.
Chỉ thị trực tiếp của người dùng có ưu tiên cao nhất. File Agent.md cũ
được giữ nguyên; khi khác nhau dùng AGENTS.md này theo yêu cầu mới.

COORDINATION_DIR: C:/Users/admin/OneDrive/Desktop/Smart_Bus-Ticket/Smart_Bus_Tickets/.coordination

Đây là đường dẫn đã có trong Agent.md và đề xuất Antigravity; không tạo
nguồn trao đổi riêng trong worktree. Prompt mới để placeholder đường dẫn,
nên tiếp tục dùng nguồn chung thực tế này.

- Codex chỉ ghi codex.md và AGREEMENT.md trong thư mục trao đổi.
- Antigravity chỉ ghi antigravity.md. Hai bên được đọc cả ba file.
- AGENTS.md được Codex khởi tạo theo yêu cầu người dùng; thay quy tắc sau
  đó phải nêu phiên bản mới và yêu cầu bên kia xác nhận.
- Trao đổi bằng file, không dùng MCP, không xác nhận thay bên kia.

## Phạm vi và quyền sở hữu

Giữ mục 1–3 đã giao cho Codex/nhóm hiện tại; không đánh số lại hay làm lại
việc nhóm đã hoàn thành. Xem nguyên văn và nguồn tại codex.md.
Giữ nguyên giao diện, thiết kế, bố cục và tính năng hiện có, trừ khi người
dùng yêu cầu khác. Sửa hành vi frontend chỉ trong thỏa thuận ACTIVE.
Không tự mở rộng sang toàn bộ backend hoặc tính năng mới.

Mỗi file chỉ một bên sửa tại một thời điểm. AGREEMENT.md phải có danh sách
đường dẫn cụ thể và người sở hữu, gồm model/DTO/project/config/controller.
Không sửa file bên kia. File cần cả hai sửa phải bàn giao nối tiếp bằng
commit, xác nhận nền mới trước khi sửa. Thêm file/phạm vi cần phiên bản
thỏa thuận mới và ACK mới.

## Cơ chế chốt

Codex tổng hợp bản có mã phiên bản và trạng thái PROPOSED, ghi rõ nhiệm vụ,
file, worktree/branch/base, phụ thuộc, giao kèo, kiểm thử và người tích hợp.
Antigravity tự ghi ACK đúng mã trong antigravity.md hoặc CHANGE_REQUEST.
Chỉ Codex chuyển ACTIVE khi đã có ACK đúng phiên bản và Codex đồng ý.
Im lặng hoặc hết thời gian không phải đồng ý. Sửa nội dung đề xuất cần
phiên bản mới. Trong lúc chờ tiếp tục việc đã được người dùng giao trước
đó và công việc độc lập; không bắt đầu phần mới phụ thuộc thỏa thuận.

## Git, dữ liệu và tích hợp

Mỗi bên làm worktree và branch riêng. Bảo toàn mọi thay đổi có sẵn; không
reset, clean, force push hoặc ghi đè. Chỉ commit file thuộc phạm vi mình.
Không tự push/deploy. Không thay route, input/output, lỗi hoặc trạng thái
dữ liệu mà chưa thống nhất; không dùng dữ liệu giả để báo thành công thật.
Không tự đổi schema/dữ liệu DB dùng chung ngoài quyền đã có. Không đưa
secret, token, mật khẩu hoặc connection string vào file trao đổi/báo cáo.

Codex là bên rà soát/tích hợp: chỉ lấy commit Antigravity đã bàn giao,
đọc diff và chạy kiểm tra phù hợp, đặc biệt regression quyền. Không tự
lấy thay đổi dở dang từ working tree bên kia.

## Báo cáo và giới hạn chờ

Mỗi bên cập nhật file của mình: nhiệm vụ, phiên bản, trạng thái, file sửa,
commit, kiểm tra/kết quả thực tế, giới hạn xác minh và yêu cầu bên kia.
Phân biệt đọc source, test mô phỏng, test SQL và HTTP thật.
Chỉ hỏi người dùng khi thay yêu cầu, phạm vi hoặc quyền thao tác; lựa chọn
kỹ thuật trong phạm vi do hai bên tự chốt. File không đánh thức AI; không
chờ vô hạn hay coi chưa có phản hồi là ACK. Ghi rõ việc đang chờ và tiếp
tục việc độc lập được phép.
