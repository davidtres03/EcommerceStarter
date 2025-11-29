// Email Preview Functionality for Branding Settings

/**
 * Preview an email template without sending
 * @param {string} emailType - Type of email: 'order', 'shipping', 'welcome', or 'test'
 */
async function previewEmail(emailType) {
    const modal = new bootstrap.Modal(document.getElementById('emailPreviewModal'));
    const previewContent = document.getElementById('emailPreviewContent');
    const previewTitle = document.getElementById('previewTitle');
    
    // Update modal title
    const titles = {
        'order': 'Order Confirmation Email',
        'shipping': 'Shipping Notification Email',
        'welcome': 'Welcome Email',
        'test': 'Test Email'
    };
    previewTitle.textContent = titles[emailType] || 'Email Preview';
    
    // Show loading state
    previewContent.innerHTML = '<div class="text-center"><span class="spinner-border"></span> Loading preview...</div>';
    modal.show();
    
    try {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const headers = { 'Content-Type': 'application/json' };
        if (tokenInput) headers['RequestVerificationToken'] = tokenInput.value;
        
        const response = await fetch('?handler=PreviewEmail', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ emailType: emailType })
        });
        
        const result = await response.json();
        
        if (result.success) {
            previewContent.innerHTML = result.html;
            // Store the email type for sending
            previewContent.dataset.emailType = emailType;
        } else {
            previewContent.innerHTML = `<div class="alert alert-danger">Error: ${result.message}</div>`;
        }
    } catch (error) {
        previewContent.innerHTML = `<div class="alert alert-danger">Error loading preview: ${error.message}</div>`;
    }
}

/**
 * Send the previewed email as a test
 */
async function sendPreviewAsTest() {
    const previewContent = document.getElementById('emailPreviewContent');
    const emailType = previewContent.dataset.emailType || 'test';
    
    const email = prompt('Enter email address to send this email to:');
    if (!email) return;
    
    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        alert('Invalid email address');
        return;
    }
    
    // Send test email
    await sendTestEmail(email);
}

/**
 * Send a test email
 * @param {string} email - Email address to send to (optional)
 */
async function sendTestEmail(email) {
    let emailAddress = email;
    
    if (!emailAddress) {
        emailAddress = prompt('Enter email address to send test email to:');
        if (!emailAddress) return;
        
        // Validate email format
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(emailAddress)) {
            showTestEmailStatus('Invalid email address format', 'danger');
            return;
        }
    }
    
    const btn = document.getElementById('testEmailBtn');
    const status = document.getElementById('testEmailStatus');
    
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
    }
    
    if (status) {
        showTestEmailStatus('Sending test email...', 'info');
    }
    
    try {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const headers = { 'Content-Type': 'application/json' };
        if (tokenInput) headers['RequestVerificationToken'] = tokenInput.value;
        
        const response = await fetch('?handler=TestEmail', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ email: emailAddress })
        });
        
        const result = await response.json();
        
        if (result && result.success) {
            showTestEmailStatus('? ' + result.message, 'success');
        } else {
            showTestEmailStatus('? ' + (result?.message || 'Failed to send test email'), 'danger');
        }
    } catch (err) {
        showTestEmailStatus('? Error: ' + err.message, 'danger');
    } finally {
        if (btn) {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-send"></i> Send Test Email';
        }
        if (status) {
            setTimeout(function() { status.innerHTML = ''; }, 8000);
        }
    }
}

/**
 * Show test email status message
 * @param {string} message - Message to display
 * @param {string} type - Alert type: 'success', 'danger', 'warning', 'info'
 */
function showTestEmailStatus(message, type) {
    const status = document.getElementById('testEmailStatus');
    if (!status) return;
    
    const alertHtml = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>`;
    status.innerHTML = alertHtml;
}
