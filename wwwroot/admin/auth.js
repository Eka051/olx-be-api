const API_BASE_URL = window.location.origin;
let userEmailForVerification = '';

document.addEventListener('DOMContentLoaded', () => {
    const requestOtpForm = document.getElementById('requestOtpForm');
    const verifyOtpForm = document.getElementById('verifyOtpForm');
    const backButton = document.getElementById('back-to-email');

    if (requestOtpForm) {
        requestOtpForm.addEventListener('submit', handleRequestOtp);
    }
    if (verifyOtpForm) {
        verifyOtpForm.addEventListener('submit', handleVerifyOtp);
        setupOtpInputs();
    }
    if (backButton) {
        backButton.addEventListener('click', () => {
            document.getElementById('otp-step').classList.add('hidden');
            document.getElementById('email-step').classList.remove('hidden');
            document.getElementById('email').value = userEmailForVerification;
        });
    }
});

async function handleRequestOtp(e) {
    e.preventDefault();
    const emailInput = document.getElementById('email');
    const email = emailInput.value;
    const errorMessage = document.getElementById('request-error-message');
    const buttonText = document.getElementById('request-button-text');
    const spinner = document.getElementById('request-loading-spinner');

    errorMessage.textContent = '';
    buttonText.classList.add('hidden');
    spinner.classList.remove('hidden');

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/email/otp`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        });

        const result = await response.json();

        if (response.status === 201 && result.success) {
            userEmailForVerification = email;
            document.getElementById('sent-to-email').textContent = email;
            document.getElementById('email-step').classList.add('hidden');
            document.getElementById('otp-step').classList.remove('hidden');
            document.querySelector('input[name="otp"]').focus();
        } else {
            errorMessage.textContent = result.message || 'Gagal mengirim OTP.';
        }
    } catch (error) {
        errorMessage.textContent = 'Tidak dapat terhubung ke server.';
    } finally {
        buttonText.classList.remove('hidden');
        spinner.classList.add('hidden');
    }
}

async function handleVerifyOtp(e) {
    e.preventDefault();
    const otpInputs = document.querySelectorAll('input[name="otp"]');
    const otp = Array.from(otpInputs).map(input => input.value).join('');
    const errorMessage = document.getElementById('verify-error-message');
    const buttonText = document.getElementById('verify-button-text');
    const spinner = document.getElementById('verify-loading-spinner');

    errorMessage.textContent = '';
    buttonText.classList.add('hidden');
    spinner.classList.remove('hidden');

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/email/verify`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: userEmailForVerification, otp })
        });

        const result = await response.json();

        if (response.ok && result.success) {
            const token = result.data.token;
            if (!token) {
                throw new Error("Token tidak ditemukan di dalam respons server.");
            }

            const tokenPayload = JSON.parse(atob(token.split('.')[1]));
            const roles = tokenPayload.role;

            const isAdmin = Array.isArray(roles) ? roles.includes('Admin') : roles === 'Admin';

            if (isAdmin) {
                localStorage.setItem('admin_jwt_token', token);
                const user = { name: tokenPayload.name, email: tokenPayload.email, profilePictureUrl: null };
                localStorage.setItem('admin_user', JSON.stringify(user));
                window.location.href = './dashboard.html';
            } else {
                errorMessage.textContent = 'Akses ditolak. Anda bukan admin.';
            }
        } else {
            errorMessage.textContent = result.message || 'Kode OTP tidak valid.';
        }
    } catch (error) {
        console.error('Error during OTP verification:', error);
        errorMessage.textContent = 'Terjadi kesalahan verifikasi.';
    } finally {
        buttonText.classList.remove('hidden');
        spinner.classList.add('hidden');
    }
}

function setupOtpInputs() {
    const inputs = document.querySelectorAll('.otp-input');
    inputs.forEach((input, index) => {
        input.addEventListener('keydown', (e) => {
            if (e.key >= 0 && e.key <= 9) {
                inputs[index].value = '';
                setTimeout(() => {
                    if (index + 1 < inputs.length) inputs[index + 1].focus();
                }, 10);
            } else if (e.key === 'Backspace') {
                setTimeout(() => {
                    if (index - 1 >= 0) inputs[index - 1].focus();
                }, 10);
            }
        });
        input.addEventListener('input', () => {
            if (input.value && index + 1 < inputs.length) {
                inputs[index + 1].focus();
            }
        });
    });
}

function checkAuth() {
    const token = localStorage.getItem('admin_jwt_token');
    if (!token && !window.location.pathname.endsWith('index.html')) {
        window.location.href = './index.html';
    } else if (token && window.location.pathname.endsWith('index.html')) {
        window.location.href = './dashboard.html';
    }
}

function logout() {
    localStorage.removeItem('admin_jwt_token');
    localStorage.removeItem('admin_user');
    window.location.href = './index.html';
}

async function fetchWithAuth(endpoint, options = {}) {
    const token = localStorage.getItem('admin_jwt_token');
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers,
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    } else {
        logout();
        throw new Error("No auth token found");
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, { ...options, headers });

    if (response.status === 401 || response.status === 403) {
        logout();
        throw new Error("Unauthorized");
    }

    if (response.headers.get("content-type")?.includes("application/json")) {
        return response.json();
    }

    return response;
}