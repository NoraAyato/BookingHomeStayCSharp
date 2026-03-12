document.addEventListener("DOMContentLoaded", () => {
    let currentEmail = '';
    const body = document.body;
    const authModal = document.getElementById('authModal');
    const otpModal = document.getElementById('otpModal');
    const successModal = document.getElementById('successModal');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    // Hiển thị modal
    const showModal = (modal) => {
        modal.classList.remove('hidden');
        body.classList.add('overflow-hidden');
    };

    // Ẩn modal
    const hideModal = (modal, callback) => {
        modal.classList.add('hidden');
        body.classList.remove('overflow-hidden');
        if (callback) callback();
    };

    // Xử lý dropdown cho "Địa điểm", "Loại chỗ ở", và menu người dùng
    const setupDropdowns = () => {
        document.querySelectorAll('[data-dropdown]').forEach(group => {
            const button = group.querySelector('button');
            const dropdownMenu = group.querySelector('.dropdown');
            const chevron = button.querySelector('.fa-chevron-down');

            if (group.dataset.dropdown === 'location' || group.dataset.dropdown === 'type') {
                // Desktop dropdown: hover để mở/đóng
                let isHovering = false;

                const showDropdown = () => {
                    dropdownMenu.classList.remove('hidden');
                    if (chevron) chevron.classList.add('rotate-180');
                    isHovering = true;
                };

                const hideDropdown = () => {
                    dropdownMenu.classList.add('hidden');
                    if (chevron) chevron.classList.remove('rotate-180');
                    isHovering = false;
                };

                // Hiển thị dropdown khi di chuột vào group
                group.addEventListener('mouseenter', () => {
                    showDropdown();
                });

                // Ẩn dropdown khi chuột rời khỏi group
                group.addEventListener('mouseleave', () => {
                    // Sử dụng setTimeout để kiểm tra nếu chuột đã rời khỏi group nhưng di vào dropdown
                    setTimeout(() => {
                        if (!isHovering) {
                            hideDropdown();
                        }
                    }, 100);
                });

                // Giữ dropdown hiển thị khi chuột di vào dropdown menu
                dropdownMenu.addEventListener('mouseenter', () => {
                    isHovering = true;
                });

                // Ẩn dropdown khi chuột rời khỏi dropdown menu
                dropdownMenu.addEventListener('mouseleave', () => {
                    isHovering = false;
                    hideDropdown();
                });

                // Đảm bảo click vào mục con không làm dropdown biến mất ngay lập tức
                dropdownMenu.querySelectorAll('a').forEach(item => {
                    item.addEventListener('click', () => {
                        // Không cần ẩn dropdown ngay lập tức, để browser xử lý navigation
                        isHovering = false;
                    });
                });
            } else if (group.dataset.dropdown === 'user') {
                // User dropdown: click để mở/đóng
                button.addEventListener('click', (e) => {
                    e.preventDefault();
                    dropdownMenu.classList.toggle('hidden');
                    if (chevron) chevron.classList.toggle('rotate-180');
                });

                // Đóng dropdown khi click bên ngoài
                document.addEventListener('click', (e) => {
                    if (!group.contains(e.target) && !dropdownMenu.classList.contains('hidden')) {
                        dropdownMenu.classList.add('hidden');
                        if (chevron) chevron.classList.remove('rotate-180');
                    }
                });
            } else if (group.dataset.dropdown === 'mobile-location' || group.dataset.dropdown === 'mobile-type') {
                // Mobile dropdown: click để mở/đóng
                button.addEventListener('click', () => {
                    dropdownMenu.classList.toggle('hidden');
                    if (chevron) chevron.classList.toggle('rotate-180');
                });
            }
        });
    };

    // Xử lý mobile menu
    const setupMobileMenu = () => {
        const mobileMenuToggle = document.getElementById('mobileMenuToggle');
        const mobileMenu = document.getElementById('mobileMenu');
        const menuOpenIcon = document.getElementById('menuOpenIcon');
        const menuCloseIcon = document.getElementById('menuCloseIcon');

        if (mobileMenuToggle && mobileMenu) {
            mobileMenuToggle.addEventListener('click', () => {
                mobileMenu.classList.toggle('hidden');
                menuOpenIcon.classList.toggle('hidden');
                menuCloseIcon.classList.toggle('hidden');
            });

            // Đóng menu khi click bên ngoài
            document.addEventListener('click', (e) => {
                if (!mobileMenu.contains(e.target) && !mobileMenuToggle.contains(e.target) && !mobileMenu.classList.contains('hidden')) {
                    mobileMenu.classList.add('hidden');
                    menuOpenIcon.classList.remove('hidden');
                    menuCloseIcon.classList.add('hidden');
                }
            });
        }
    };

    // Khởi tạo các chức năng
    setupDropdowns();
    setupMobileMenu();

    // Hiển thị modal đăng nhập/đăng ký
    window.showAuthModal = (tab) => {
        showModal(authModal);
        if (tab === 'login') showLoginForm();
        else if (tab === 'register') showRegisterForm();
        else if (tab === 'forgot') showForgotPasswordForm();
    };

    // Ẩn modal đăng nhập/đăng ký
    window.hideAuthModal = () => {
        hideModal(authModal, () => {
            document.querySelectorAll('.tab-content').forEach(tab => tab.classList.add('hidden'));
            document.getElementById('loginForm').classList.remove('hidden');
            document.querySelectorAll('[id$="Error"]').forEach(el => el.textContent = '');
            document.getElementById('loginErrorMessage').textContent = '';
            document.getElementById('registerErrorMessage').textContent = '';
            document.getElementById('forgotPasswordMessage').textContent = '';
        });
    };

    // Hiển thị form đăng nhập
    window.showLoginForm = () => {
        document.getElementById('authModalTitle').textContent = 'Đăng nhập tài khoản';
        document.getElementById('socialLoginText').textContent = 'Đăng nhập với Google';
        document.getElementById('dividerText').textContent = 'hoặc đăng nhập với email';
        document.getElementById('authFooterText').textContent = 'Chưa có tài khoản?';
        document.getElementById('toggleAuthLink').textContent = 'Đăng ký ngay';
        document.getElementById('toggleAuthLink').onclick = showRegisterForm;

        document.getElementById('loginForm').classList.remove('hidden');
        document.getElementById('registerForm').classList.add('hidden');
        document.getElementById('forgotPasswordForm').classList.add('hidden');

        document.getElementById('socialLoginSection').classList.remove('hidden');
        document.getElementById('dividerSection').classList.remove('hidden');
    };

    // Hiển thị form đăng ký
    window.showRegisterForm = () => {
        document.getElementById('authModalTitle').textContent = 'Đăng ký tài khoản';
        document.getElementById('socialLoginText').textContent = 'Đăng ký với Google';
        document.getElementById('dividerText').textContent = 'hoặc đăng ký với email';
        document.getElementById('authFooterText').textContent = 'Bạn đã có tài khoản?';
        document.getElementById('toggleAuthLink').textContent = 'Đăng nhập';
        document.getElementById('toggleAuthLink').onclick = showLoginForm;

        document.getElementById('loginForm').classList.add('hidden');
        document.getElementById('registerForm').classList.remove('hidden');
        document.getElementById('forgotPasswordForm').classList.add('hidden');

        document.getElementById('socialLoginSection').classList.remove('hidden');
        document.getElementById('dividerSection').classList.remove('hidden');
    };

    // Hiển thị form quên mật khẩu
    window.showForgotPasswordForm = () => {
        document.getElementById('authModalTitle').textContent = 'Quên mật khẩu';
        document.getElementById('authFooterText').textContent = 'Quay lại';
        document.getElementById('toggleAuthLink').textContent = 'Đăng nhập';
        document.getElementById('toggleAuthLink').onclick = showLoginForm;

        document.getElementById('loginForm').classList.add('hidden');
        document.getElementById('registerForm').classList.add('hidden');
        document.getElementById('forgotPasswordForm').classList.remove('hidden');

        document.getElementById('socialLoginSection').classList.add('hidden');
        document.getElementById('dividerSection').classList.add('hidden');
    };

    // Chuyển đổi giữa form đăng nhập và đăng ký
    window.toggleAuthForm = () => {
        document.getElementById('loginForm').classList.contains('hidden') ? showLoginForm() : showRegisterForm();
    };

    // Hiển thị thông báo thành công
    window.showSuccessModal = (title, message, callback) => {
        document.getElementById('success-modal-title').textContent = title;
        document.getElementById('success-modal-message').textContent = message;
        showModal(successModal);
        setTimeout(() => hideModal(successModal, callback), 2000);
    };

    // Xử lý đăng ký
    const registerForm = document.getElementById("registerForm");
    if (registerForm) {
        registerForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            // Reset error messages
            document.querySelectorAll('[id$="Error"]').forEach(el => el.textContent = '');
            document.getElementById("registerErrorMessage").textContent = '';

            const email = document.getElementById("registerEmail").value;
            const password = document.getElementById("registerPassword").value;
            const confirmPassword = document.getElementById("confirmPassword").value;
            const fullName = document.getElementById("fullName").value;

            // Client-side validation
            const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;
            const namePattern = /^[a-zA-Z\s]{2,}$/;

            if (!emailPattern.test(email)) {
                document.getElementById("emailError").textContent = "Email không hợp lệ.";
                return;
            }
            if (!passwordPattern.test(password)) {
                document.getElementById("passwordError").textContent = "Mật khẩu phải có ít nhất 6 ký tự, chứa 1 chữ cái in hoa, 1 chữ thường, 1 số.";
                return;
            }
            if (password !== confirmPassword) {
                document.getElementById("confirmPasswordError").textContent = "Xác nhận mật khẩu không khớp.";
                return;
            }
            if (!namePattern.test(fullName)) {
                document.getElementById("fullNameError").textContent = "Họ và tên phải có ít nhất 2 ký tự và không chứa ký tự đặc biệt.";
                return;
            }

            const formData = new FormData(registerForm);
            try {
                const response = await fetch(registerForm.action, {
                    method: 'POST',
                    body: formData,
                    headers: { 'RequestVerificationToken': token }
                });

                const result = await response.json();
                if (result.success) {
                    currentEmail = email;
                    showOtpModal();
                } else {
                    document.getElementById("registerErrorMessage").textContent = result.errors?.join(' ') || result.message;
                    result.errors?.forEach(error => {
                        if (error.includes("Email")) document.getElementById("emailError").textContent = error;
                        else if (error.includes("Mật khẩu")) document.getElementById("passwordError").textContent = error;
                        else if (error.includes("Xác nhận")) document.getElementById("confirmPasswordError").textContent = error;
                        else if (error.includes("Họ và tên")) document.getElementById("fullNameError").textContent = error;
                    });
                }
            } catch {
                document.getElementById("registerErrorMessage").textContent = 'Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.';
            }
        });
    }

    // Xử lý OTP
    window.showOtpModal = () => {
        showModal(otpModal);
        document.getElementById('otpInput').value = '';
        document.getElementById('otpError').classList.add('hidden');
        startOtpCountdown();
    };

    window.hideOtpModal = () => {
        hideModal(otpModal);
        clearInterval(window.otpCountdownInterval);
    };

    window.startOtpCountdown = () => {
        let timeLeft = 60;
        const countdownElement = document.getElementById('otpCountdown');
        const resendButton = document.getElementById('resendOtpButton');
        resendButton.disabled = true;

        clearInterval(window.otpCountdownInterval);
        window.otpCountdownInterval = setInterval(() => {
            countdownElement.textContent = --timeLeft;
            if (timeLeft <= 0) {
                clearInterval(window.otpCountdownInterval);
                resendButton.disabled = false;
                countdownElement.textContent = '60';
            }
        }, 1000);
    };

    window.restrictToNumbers = (input) => {
        input.value = input.value.replace(/[^0-9]/g, '');
    };

    window.resendOtp = async () => {
        document.getElementById('otpInput').value = '';
        document.getElementById('otpError').classList.add('hidden');

        const formData = new FormData(document.getElementById('registerForm'));
        try {
            const response = await fetch('/Account/Register', {
                method: 'POST',
                body: formData,
                headers: { 'RequestVerificationToken': token }
            });
            const result = await response.json();
            if (result.success) startOtpCountdown();
            else {
                document.getElementById('otpError').textContent = result.message;
                document.getElementById('otpError').classList.remove('hidden');
            }
        } catch {
            document.getElementById('otpError').textContent = 'Lỗi khi gửi lại OTP.';
            document.getElementById('otpError').classList.remove('hidden');
        }
    };

    window.verifyOtp = async () => {
        const otp = document.getElementById('otpInput').value;
        const otpError = document.getElementById('otpError');

        if (!/^\d{6}$/.test(otp)) {
            otpError.textContent = 'Mã OTP phải là 6 chữ số.';
            otpError.classList.remove('hidden');
            return;
        }

        try {
            const response = await fetch('/Account/VerifyRegisterOtp', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ email: currentEmail, otp })
            });

            const result = await response.json();
            if (result.success) {
                hideOtpModal();
                showSuccessModal('Đăng ký thành công', 'Tài khoản của bạn đã được tạo thành công!', () => {
                    showLoginForm();
                    document.getElementById('loginEmail').value = result.email;
                    document.getElementById('loginPassword').value = result.password;
                });
            } else {
                otpError.textContent = result.message || 'Mã OTP không đúng.';
                otpError.classList.remove('hidden');
            }
        } catch {
            otpError.textContent = 'Lỗi khi xác minh OTP.';
            otpError.classList.remove('hidden');
        }
    };

    // Xử lý đăng nhập
    const loginForm = document.getElementById("loginForm");
    if (loginForm) {
        loginForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            const formData = {
                Email: document.getElementById("loginEmail").value,
                Password: document.getElementById("loginPassword").value,
                RememberMe: document.getElementById("remember-me").checked
            };

            try {
                const response = await fetch("/Account/Login", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(formData)
                });

                const data = await response.json();
                if (data.success) {
                    hideAuthModal();
                    showSuccessModal('Đăng nhập thành công', 'Chào mừng bạn trở lại!', () => {
                        window.location.href = data.redirectUrl;
                    });
                } else {
                    document.getElementById("loginErrorMessage").textContent = data.message;
                    if (data.message.includes("Google")) {
                        showSuccessModal('Thông báo', data.message, showLoginForm);
                    }
                }
            } catch {
                document.getElementById("loginErrorMessage").textContent = "Lỗi khi đăng nhập. Vui lòng thử lại.";
            }
        });
    }

    // Xử lý quên mật khẩu
    const forgotPasswordForm = document.getElementById("forgotPasswordForm");
    if (forgotPasswordForm) {
        forgotPasswordForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            const email = document.getElementById("forgotEmail").value;
            const btn = document.getElementById("forgotPasswordBtn");
            const messageDiv = document.getElementById("forgotPasswordMessage");

            btn.innerHTML = "Đang gửi...";
            btn.disabled = true;

            try {
                const response = await fetch("/Account/ForgotPassword", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ Email: email })
                });

                const data = await response.json();
                messageDiv.innerHTML = `<p class="${data.success ? 'text-green-600' : 'text-red-600'}">${data.message}</p>`;
            } catch {
                messageDiv.innerHTML = `<p class="text-red-600">Có lỗi xảy ra. Vui lòng thử lại!</p>`;
            } finally {
                btn.innerHTML = "Gửi yêu cầu";
                btn.disabled = false;
            }
        });
    }

    // Đóng modal khi nhấn ESC
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            hideAuthModal();
            hideOtpModal();
            hideModal(successModal);
        }
    });
    window.hideSuccessModal = () => {
        hideModal(successModal);
    };

    // Đóng modal khi nhấn ra ngoài
    document.addEventListener('click', (event) => {
        if (event.target === authModal) hideAuthModal();
        if (event.target === otpModal) hideOtpModal();
        if (event.target === successModal) hideModal(successModal);
    });

    // Cập nhật nút đăng nhập trên navigation
    window.showLoginModal = () => showAuthModal('login');
});