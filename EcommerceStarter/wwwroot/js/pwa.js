// PWA installation prompt and service worker registration
(function() {
    let deferredPrompt;
    const INSTALL_DISMISSED_KEY = 'pwa_install_dismissed';
    
    // Register service worker
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js')
                .then(registration => {
                })
                .catch(err => {
                });
        });
    }
    
    // Handle PWA install prompt
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        
        // Don't show if user previously dismissed
        if (localStorage.getItem(INSTALL_DISMISSED_KEY) === 'true') {
            return;
        }
        
        // Show install banner after 2 minutes (less intrusive)
        setTimeout(() => showInstallBanner(), 120000);
    });
    
    function showInstallBanner() {
        const banner = document.createElement('div');
        banner.className = 'pwa-install-banner';
        banner.innerHTML = `
            <div class="pwa-install-content">
                <div class="pwa-install-icon">
                    <i class="bi bi-phone"></i>
                </div>
                <div class="pwa-install-text">
                    <h5>Install Our App</h5>
                    <p>Get quick access and offline browsing</p>
                </div>
                <div class="pwa-install-actions">
                    <button class="btn btn-primary btn-sm" onclick="installPWA()">Install</button>
                    <button class="btn btn-link btn-sm" onclick="dismissPWA()">Not Now</button>
                </div>
            </div>
        `;
        
        document.body.appendChild(banner);
        setTimeout(() => banner.classList.add('show'), 100);
    }
    
    window.installPWA = function() {
        const banner = document.querySelector('.pwa-install-banner');
        if (banner) banner.remove();
        
        if (deferredPrompt) {
            deferredPrompt.prompt();
            deferredPrompt.userChoice.then((choiceResult) => {
                deferredPrompt = null;
            });
        }
    };
    
    window.dismissPWA = function() {
        const banner = document.querySelector('.pwa-install-banner');
        if (banner) {
            banner.classList.remove('show');
            setTimeout(() => banner.remove(), 300);
        }
        localStorage.setItem(INSTALL_DISMISSED_KEY, 'true');
    };
    
    // Handle successful installation
    window.addEventListener('appinstalled', () => {
        deferredPrompt = null;
    });
})();
