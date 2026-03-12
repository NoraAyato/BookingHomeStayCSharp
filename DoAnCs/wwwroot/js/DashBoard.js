tailwind.config = {
    theme: {
        extend: {
            colors: {
                primary: {
                    50: '#f0f9ff',
                    100: '#e0f2fe',
                    200: '#bae6fd',
                    300: '#7dd3fc',
                    400: '#38bdf8',
                    500: '#0ea5e9',
                    600: '  #0284c7',
                    700: '#0369a1',
                    800: '#075985',
                    900: '#0c4a6e',
                },
                secondary: {
                    500: '#64748b',
                    600: '#475569',
                    700: '#334155',
                }
            },
            fontFamily: {
                sans: ['Inter', 'ui-sans-serif', 'system-ui'],
            },
        }
    }
}

// Toggle advanced filters
document.getElementById('toggleAdvancedFilters').addEventListener('click', function () {
    const advFilters = document.getElementById('advancedFilters');
    const icon = this.querySelector('.fa-chevron-down');

    advFilters.classList.toggle('hidden');
    icon.classList.toggle('fa-chevron-down');
    icon.classList.toggle('fa-chevron-up');
});

// Initialize tooltips
document.querySelectorAll('[title]').forEach(el => {
    new bootstrap.Tooltip(el); // Requires Bootstrap JS
});

// Sample function to handle user actions
document.querySelectorAll('.user-table-row').forEach(row => {
    row.addEventListener('click', (e) => {
        // Ignore if clicking on action buttons
        if (!e.target.closest('button')) {
            console.log('View user details');
            // Implement view user details functionality
        }
    });
});

document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.querySelector('.fixed.inset-y-0');
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebarOverlay = document.getElementById('sidebar-overlay');
    const logoText = document.getElementById('logo-text');
    const userInfo = document.getElementById('user-info');
    const searchBox = document.getElementById('search-box');
    let isCollapsed = false;

    // Toggle sidebar trên mobile
    sidebarToggle.addEventListener('click', function () {
        sidebar.classList.toggle('-translate-x-full');
        sidebarOverlay.style.display = sidebar.classList.contains('-translate-x-full') ? 'none' : 'block';
    });

    // Đóng sidebar khi click overlay
    sidebarOverlay.addEventListener('click', function () {
        sidebar.classList.add('-translate-x-full');
        this.style.display = 'none';
    });

    // Toggle sidebar collapse trên desktop
    function toggleSidebar() {
        isCollapsed = !isCollapsed;

        if (isCollapsed) {
            sidebar.classList.add('w-20');
            logoText.classList.add('opacity-0');
            userInfo.classList.add('opacity-0');
            searchBox.classList.add('opacity-0', 'pointer-events-none');
        } else {
            sidebar.classList.remove('w-20');
            logoText.classList.remove('opacity-0');
            userInfo.classList.remove('opacity-0');
            searchBox.classList.remove('opacity-0', 'pointer-events-none');
        }

        localStorage.setItem('sidebarCollapsed', isCollapsed);
    }

    // Kiểm tra trạng thái sidebar từ localStorage
    if (localStorage.getItem('sidebarCollapsed') === 'true') {
        toggleSidebar();
    }

    // Tooltip khi sidebar collapsed
    tippy('[data-tippy-content]', {
        placement: 'right',
        appendTo: document.body,
        animation: 'shift-away',
        delay: [100, 0],
        theme: 'light-border',
    });

    // Xử lý active menu
    const currentPath = window.location.pathname;
    document.querySelectorAll('nav a').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('bg-primary-50', 'text-primary-700');
            link.querySelector('i').classList.add('text-primary-600');
        }
    });
});

// Alpine.js cho menu dropdown
document.addEventListener('alpine:init', () => {
    Alpine.data('sidebar', () => ({
        init() {
            // Kiểm tra menu mở theo route hiện tại
            const currentPath = window.location.pathname;
            this.open = this.$el.querySelector(`a[href="${currentPath}"]`) !== null;
        }
    }));
});


//
}