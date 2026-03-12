// js/common.js
function loadSidebar() {
    fetch('sidebar.html')
        .then(response => response.text())
        .then(data => {
            // Chèn nội dung sidebar vào placeholder
            document.getElementById('sidebar-container').innerHTML = data;
            highlightActiveMenu(); // Gọi hàm highlight menu
        })
        .catch(error => console.error("Lỗi tải sidebar:", error));
}

function highlightActiveMenu() {
    // Lấy tên trang hiện tại (ví dụ: "index.html" -> "index")
    const currentPage = window.location.pathname
        .split('/') // Tách đường dẫn thành mảng
        .pop() // Lấy phần cuối cùng (tên file)
        .replace('.html', ''); // Loại bỏ ".html"

    // Tìm tất cả các menu trong sidebar
    const menuItems = document.querySelectorAll('nav a');

    // Duyệt qua từng menu
    menuItems.forEach(menu => {
        const menuPage = menu.getAttribute('data-page'); // Lấy giá trị data-page

        // Nếu menu trùng với trang hiện tại
        if (menuPage === currentPage) {
            // Thêm class active
            menu.classList.add('bg-blue-50', 'text-blue-600');
            menu.classList.remove('text-gray-600', 'hover:bg-gray-100');
        } else {
            // Xóa class active (nếu có)
            menu.classList.remove('bg-blue-50', 'text-blue-600');
            menu.classList.add('text-gray-600', 'hover:bg-gray-100');
        }
    });
}

// Khởi chạy khi trang được tải
document.addEventListener('DOMContentLoaded', loadSidebar);