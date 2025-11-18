// Secure Key Management UI for Branding Settings
// Handles showing/hiding sensitive keys with edit capability

// Resend API Key Management
function toggleResendKeyEdit() {
    document.getElementById('resendKeyEditContainer').style.display = 'block';
    document.getElementById('resendApiKeyEdit').focus();
}

function saveResendKeyEdit() {
    const newKey = document.getElementById('resendApiKeyEdit').value.trim();
    
    if (!newKey) {
        alert('Please enter a new API key or click Cancel to keep the existing one.');
        return;
    }
    
    // Update the actual form field
    const hiddenInput = document.querySelector('input[name="Settings.ResendApiKey"]');
    if (hiddenInput) {
        hiddenInput.value = newKey;
    }
    
    // Hide edit UI
    document.getElementById('resendKeyEditContainer').style.display = 'none';
    document.getElementById('resendApiKeyEdit').value = '';
    
    // Show success feedback
    showToast('Success', 'New API key will be saved when you submit the form', 'info');
}

function cancelResendKeyEdit() {
    document.getElementById('resendKeyEditContainer').style.display = 'none';
    document.getElementById('resendApiKeyEdit').value = '';
}

// SMTP Password Management
function toggleSmtpPasswordEdit() {
    document.getElementById('smtpPasswordEditContainer').style.display = 'block';
    document.getElementById('smtpPasswordEdit').focus();
}

function saveSmtpPasswordEdit() {
    const newPassword = document.getElementById('smtpPasswordEdit').value.trim();
    
    if (!newPassword) {
        alert('Please enter a new password or click Cancel to keep the existing one.');
        return;
    }
    
    // Update the actual form field
    const hiddenInput = document.querySelector('input[name="Settings.SmtpPassword"]');
    if (hiddenInput) {
        hiddenInput.value = newPassword;
    }
    
    // Hide edit UI
    document.getElementById('smtpPasswordEditContainer').style.display = 'none';
    document.getElementById('smtpPasswordEdit').value = '';
    
    // Show success feedback
    showToast('Success', 'New password will be saved when you submit the form', 'info');
}

function cancelSmtpPasswordEdit() {
    document.getElementById('smtpPasswordEditContainer').style.display = 'none';
    document.getElementById('smtpPasswordEdit').value = '';
}
