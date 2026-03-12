// validation.js
function isValidEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function isValidPhoneNumber(phone) {
    const re = /^[0-9]{10,15}$/;
    return re.test(phone);
}