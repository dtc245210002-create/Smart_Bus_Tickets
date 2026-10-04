document.addEventListener('DOMContentLoaded',()=>{
    const trips=document.getElementById('driverTrip'),date=document.getElementById('driverDate'),input=document.getElementById('ticketInput');
    function sync(){date.value=trips.selectedOptions[0]?.dataset.date||'';} sync();trips.addEventListener('change',sync);
    document.querySelectorAll('.demo-ticket-fill').forEach(b=>b.addEventListener('click',()=>{input.value=b.dataset.ticket;trips.selectedIndex=[...trips.options].findIndex(o=>o.value===b.dataset.trip&&o.dataset.date===b.dataset.date);date.value=b.dataset.date;input.focus();}));
    const video=document.getElementById('scannerVideo'),message=document.getElementById('scannerMessage'),stopButton=document.getElementById('stopScanner');let stream=null,scanning=false;
    function stop(){scanning=false;stream?.getTracks().forEach(t=>t.stop());stream=null;video.srcObject=null;video.classList.add('d-none');stopButton.classList.add('d-none');}
    document.getElementById('startScanner').addEventListener('click',async()=>{
        if(scanning)return;if(!('BarcodeDetector' in window)||!navigator.mediaDevices?.getUserMedia){message.textContent='Trình duyệt chưa hỗ trợ quét QR trực tiếp. Nhập mã vé hoặc bấm “Dùng mã này” trong danh sách.';return;}
        try { const detector=new BarcodeDetector({formats:['qr_code']});stream=await navigator.mediaDevices.getUserMedia({video:{facingMode:'environment'}});video.srcObject=stream;video.classList.remove('d-none');stopButton.classList.remove('d-none');await video.play();scanning=true;message.textContent='Đưa mã QR vào camera. Sau khi đọc mã, bấm Kiểm tra & check-in.';
            async function scan(){if(!scanning)return;try{const results=await detector.detect(video);if(results.length){input.value=results[0].rawValue;stop();message.textContent='Đã đọc mã. Bấm Kiểm tra & check-in để xác thực.';return;}}catch{stop();message.textContent='Không thể đọc camera. Vui lòng nhập mã vé.';return;}if(scanning)setTimeout(scan,400);}scan();
        }catch{stop();message.textContent='Không mở được camera. Cho phép quyền camera hoặc nhập mã vé thủ công.';}
    });stopButton.addEventListener('click',stop);window.addEventListener('pagehide',stop);
});
