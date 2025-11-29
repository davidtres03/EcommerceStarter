// ========================================
// DARK MODE MANAGEMENT
// ========================================
const ThemeManager = {
    init() {
        // Check for saved theme preference or default to system preference
        const savedTheme = localStorage.getItem('theme');
        const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        
        if (savedTheme) {
            this.setTheme(savedTheme);
        } else if (systemPrefersDark) {
            this.setTheme('dark');
        } else {
            this.setTheme('light');
        }
        
        // Listen for system theme changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            // Only auto-switch if user hasn't manually set a preference
            if (!localStorage.getItem('theme')) {
                this.setTheme(e.matches ? 'dark' : 'light');
            }
        });
    },
    
    setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        
        // Update toggle button icon if it exists
        this.updateToggleIcon(theme);
        
        // Trigger smooth transition effect
        this.addTransitionEffect();
    },
    
    toggleTheme() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
        
        // Animate the toggle button
        const btn = document.getElementById('themeToggleBtn');
        if (btn) {
            btn.classList.add('theme-toggle-active');
            setTimeout(() => {
                btn.classList.remove('theme-toggle-active');
            }, 400);
        }
        
        // Show toast notification
        if (window.ToastManager) {
            const message = newTheme === 'dark' 
                ? '?? Dark mode enabled' 
                : '?? Light mode enabled';
            ToastManager.info(message, '', 2000);
        }
    },
    
    getCurrentTheme() {
        return document.documentElement.getAttribute('data-theme') || 'light';
    },
    
    updateToggleIcon(theme) {
        const iconElement = document.querySelector('.theme-toggle-icon');
        if (iconElement) {
            iconElement.className = theme === 'dark' 
                ? 'bi bi-moon-fill theme-toggle-icon' 
                : 'bi bi-sun-fill theme-toggle-icon';
        }
    },
    
    addTransitionEffect() {
        // Add a smooth transition to the entire page
        const html = document.documentElement;
        html.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        
        setTimeout(() => {
            html.style.transition = '';
        }, 300);
    },
    
    createToggleButton() {
        const toggle = document.createElement('button');
        toggle.className = 'theme-toggle';
        toggle.setAttribute('aria-label', 'Toggle dark mode');
        toggle.setAttribute('title', 'Toggle dark mode');
        
        const currentTheme = this.getCurrentTheme();
        const icon = currentTheme === 'dark' ? 'bi-moon-fill' : 'bi-sun-fill';
        
        toggle.innerHTML = `
            <span class="theme-toggle-slider">
                <i class="bi ${icon} theme-toggle-icon"></i>
            </span>
        `;
        
        toggle.addEventListener('click', () => this.toggleTheme());
        
        return toggle;
    }
};

// ========================================
// TOAST NOTIFICATION SYSTEM
// ========================================
const ToastManager = {
    container: null,
    
    init() {
        // Create toast container if it doesn't exist
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container';
            document.body.appendChild(this.container);
        }
    },
    
    show(message, title = '', type = 'info', duration = 4000) {
        this.init();
        
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        
        const iconMap = {
            success: 'bi-check-circle-fill',
            error: 'bi-x-circle-fill',
            warning: 'bi-exclamation-triangle-fill',
            info: 'bi-info-circle-fill'
        };
        
        toast.innerHTML = `
            <i class="bi ${iconMap[type]} toast-icon"></i>
            <div class="toast-content">
                ${title ? `<div class="toast-title">${title}</div>` : ''}
                <p class="toast-message">${message}</p>
            </div>
            <button class="toast-close" aria-label="Close">×</button>
        `;
        
        this.container.appendChild(toast);
        
        // Close button handler
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => this.hide(toast));
        
        // Auto-hide after duration
        if (duration > 0) {
            setTimeout(() => this.hide(toast), duration);
        }
        
        return toast;
    },
    
    hide(toast) {
        toast.classList.add('toast-exit');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    },
    
    success(message, title = 'Success') {
        return this.show(message, title, 'success');
    },
    
    error(message, title = 'Error') {
        return this.show(message, title, 'error');
    },
    
    warning(message, title = 'Warning') {
        return this.show(message, title, 'warning');
    },
    
    info(message, title = '') {
        return this.show(message, title, 'info');
    }
};

// ========================================
// STICKY NAVIGATION
// ========================================
function initStickyNav() {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;
    
    let lastScroll = 0;
    const navbarHeight = navbar.offsetHeight;
    
    window.addEventListener('scroll', () => {
        const currentScroll = window.scrollY;
        
        if (currentScroll > navbarHeight) {
            navbar.classList.add('navbar-sticky');
            document.body.classList.add('has-sticky-nav');
        } else {
            navbar.classList.remove('navbar-sticky');
            document.body.classList.remove('has-sticky-nav');
        }
        
        lastScroll = currentScroll;
    });
}

// ========================================
// CART BADGE BOUNCE ANIMATION
// ========================================
function animateCartBadge() {
    const cartBadge = document.querySelector('.nav-link .badge');
    if (!cartBadge) return;
    
    cartBadge.classList.add('bounce');
    setTimeout(() => {
        cartBadge.classList.remove('bounce');
    }, 600);
}

// Override add to cart to show toast and animate badge
function enhanceAddToCart() {
    // Listen for successful add to cart
    const addToCartForms = document.querySelectorAll('form[action*="AddToCart"]');
    
    addToCartForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            // Don't prevent default - let form submit
            // But show feedback after a short delay
            setTimeout(() => {
                animateCartBadge();
                updateFloatingCart(); // Update floating cart widget
                ToastManager.success('Item added to your cart', 'Added to Cart');
            }, 100);
        });
    });
}

// ========================================
// SMOOTH SCROLL FOR ANCHOR LINKS
// ========================================
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#') return;
            
            e.preventDefault();
            const target = document.querySelector(href);
            
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// ========================================
// LOADING SKELETON MANAGEMENT
// ========================================
const SkeletonLoader = {
    create(type = 'card') {
        if (type === 'product-card') {
            return `
                <div class="product-card-skeleton">
                    <div class="skeleton skeleton-img"></div>
                    <div class="skeleton-card">
                        <div class="skeleton skeleton-title"></div>
                        <div class="skeleton skeleton-text"></div>
                        <div class="skeleton skeleton-text" style="width: 60%"></div>
                        <div class="skeleton skeleton-button mt-3"></div>
                    </div>
                </div>
            `;
        }
        return '<div class="skeleton skeleton-text"></div>';
    },
    
    show(container, count = 1, type = 'product-card') {
        const skeletons = Array(count).fill(null).map(() => this.create(type)).join('');
        container.innerHTML = skeletons;
    },
    
    hide(container) {
        const skeletons = container.querySelectorAll('.skeleton, .product-card-skeleton');
        skeletons.forEach(skeleton => skeleton.remove());
    }
};

// ========================================
// ALERT AUTO-DISMISS
// ========================================
function initAlertAutoDismiss() {
    // Only auto-dismiss alerts that are explicitly marked as dismissible AND temporary
    const alerts = document.querySelectorAll('.alert.alert-dismissible.alert-temporary');
    
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
}

// ========================================
// IMAGE LAZY LOADING
// ========================================
function initLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');
    
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
                observer.unobserve(img);
            }
        });
    });
    
    images.forEach(img => imageObserver.observe(img));
}

// ========================================
// FORM VALIDATION ENHANCEMENT
// ========================================
function enhanceFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
                
                // Show error toast
                const invalidFields = form.querySelectorAll(':invalid');
                if (invalidFields.length > 0) {
                    ToastManager.error('Please fill in all required fields correctly', 'Form Validation Error');
                }
            }
            
            form.classList.add('was-validated');
        });
        
        // Real-time validation feedback
        const inputs = form.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                if (this.checkValidity()) {
                    this.classList.remove('is-invalid');
                    this.classList.add('is-valid');
                } else {
                    this.classList.remove('is-valid');
                    this.classList.add('is-invalid');
                }
            });
        });
    });
}

// ========================================
// QUANTITY INPUT ENHANCEMENT
// ========================================
function enhanceQuantityInputs() {
    const quantityInputs = document.querySelectorAll('input[type="number"][name="quantity"]');
    
    quantityInputs.forEach(input => {
        input.addEventListener('change', function() {
            const min = parseInt(this.min) || 1;
            const max = parseInt(this.max) || 999;
            let value = parseInt(this.value);
            
            if (value < min) {
                this.value = min;
                ToastManager.warning(`Minimum quantity is ${min}`, 'Quantity Updated');
            } else if (value > max) {
                this.value = max;
                ToastManager.warning(`Maximum available quantity is ${max}`, 'Quantity Limited');
            }
        });
    });
}

// ========================================
// BACK TO TOP BUTTON
// ========================================
function initBackToTop() {
    const backToTop = document.createElement('button');
    backToTop.className = 'btn btn-primary btn-back-to-top';
    backToTop.innerHTML = '<i class="bi bi-arrow-up"></i>';
    backToTop.setAttribute('aria-label', 'Back to top');
    backToTop.setAttribute('title', 'Back to top');
    backToTop.style.display = 'none';
    
    document.body.appendChild(backToTop);
    
    window.addEventListener('scroll', () => {
        if (window.scrollY > 300) {
            backToTop.style.display = 'flex';
        } else {
            backToTop.style.display = 'none';
        }
    });
    
    backToTop.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

// ========================================
// CONFIRM DELETE ACTIONS
// ========================================
function initDeleteConfirmation() {
    const deleteForms = document.querySelectorAll('form[action*="Delete"], form[asp-page-handler="Delete"]');
    
    deleteForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!confirm('Are you sure you want to delete this item? This action cannot be undone.')) {
                e.preventDefault();
            }
        });
    });
}

// ========================================
// SEARCH ENHANCEMENT
// ========================================
function enhanceSearch() {
    const searchInputs = document.querySelectorAll('input[type="search"]');
    
    searchInputs.forEach(input => {
        // Add clear button
        const clearBtn = document.createElement('button');
        clearBtn.className = 'btn btn-sm btn-link position-absolute end-0 top-50 translate-middle-y';
        clearBtn.innerHTML = '<i class="bi bi-x"></i>';
        clearBtn.style.display = 'none';
        
        input.parentElement.style.position = 'relative';
        input.parentElement.appendChild(clearBtn);
        
        input.addEventListener('input', function() {
            clearBtn.style.display = this.value ? 'block' : 'none';
        });
        
        clearBtn.addEventListener('click', function(e) {
            e.preventDefault();
            input.value = '';
            clearBtn.style.display = 'none';
            input.focus();
        });
    });
}

// ========================================
// FLOATING CART WIDGET (Mobile)
// ========================================
const FloatingCartWidget = {
    widget: null,
    cartLink: null,
    
    init() {
        // Only show on mobile/tablet (below 992px)
        if (window.innerWidth >= 992) return;
        
        this.createWidget();
        this.updateBadge();
        
        // Listen for window resize to hide/show based on screen size
        window.addEventListener('resize', () => {
            if (window.innerWidth >= 992) {
                if (this.widget) this.widget.style.display = 'none';
            } else {
                if (this.widget) this.widget.style.display = 'flex';
            }
        });
    },
    
    createWidget() {
        // Check if widget already exists
        if (document.querySelector('.floating-cart-widget')) {
            this.widget = document.querySelector('.floating-cart-widget');
            return;
        }
        
        this.widget = document.createElement('a');
        this.widget.href = '/Cart/Index';
        this.widget.className = 'floating-cart-widget';
        this.widget.setAttribute('title', 'View Shopping Cart');
        this.widget.setAttribute('aria-label', 'Shopping Cart');
        this.widget.innerHTML = `
            <i class="bi bi-cart3"></i>
            <span class="floating-cart-badge" style="display: none;">0</span>
        `;
        
        document.body.appendChild(this.widget);
        
        // Only show on mobile
        if (window.innerWidth < 992) {
            this.widget.style.display = 'flex';
        } else {
            this.widget.style.display = 'none';
        }
    },
    
    updateBadge() {
        const badge = document.querySelector('.floating-cart-badge');
        const navBadge = document.querySelector('.nav-link .badge');
        
        if (badge && navBadge) {
            const count = navBadge.textContent.trim();
            badge.textContent = count;
            badge.style.display = count > 0 ? 'flex' : 'none';
        }
    },
    
    animate() {
        if (!this.widget) return;
        this.widget.classList.add('cart-bounce');
        setTimeout(() => {
            this.widget.classList.remove('cart-bounce');
        }, 600);
    }
};

// ========================================
// UPDATE FLOATING CART ON ADD
// ========================================
function updateFloatingCart() {
    if (window.FloatingCartWidget) {
        FloatingCartWidget.updateBadge();
        FloatingCartWidget.animate();
    }
}

// ========================================
// MOBILE DROPDOWN SCROLL FIX
// ========================================
function fixMobileDropdownScroll() {
    // Get all dropdowns
    const dropdowns = document.querySelectorAll('.dropdown');
    
    dropdowns.forEach(dropdown => {
        // Listen for when dropdown is shown
        dropdown.addEventListener('show.bs.dropdown', function() {
            // Prevent body scroll when dropdown is open on mobile
            if (window.innerWidth < 992) {
                document.body.style.overflow = 'hidden';
            }
        });
        
        // Listen for when dropdown is hidden
        dropdown.addEventListener('hide.bs.dropdown', function() {
            // Re-enable body scroll
            document.body.style.overflow = '';
        });
        
        // Also handle when dropdown menu is clicked
        const dropdownMenu = dropdown.querySelector('.dropdown-menu');
        if (dropdownMenu) {
            dropdownMenu.addEventListener('click', function(e) {
                // If clicking a dropdown item (not just the menu), allow it to close normally
                if (e.target.classList.contains('dropdown-item')) {
                    document.body.style.overflow = '';
                }
            });
        }
    });
    
    // Handle window resize
    window.addEventListener('resize', () => {
        if (window.innerWidth >= 992) {
            // Re-enable scroll on larger screens
            document.body.style.overflow = '';
        }
    });
}

// ========================================
// INITIALIZE ALL ENHANCEMENTS
// ========================================
document.addEventListener('DOMContentLoaded', function() {
    // Initialize theme FIRST (before any other features)
    ThemeManager.init();

    // Attach theme toggle handler (moved from inline onclick in layout for CSP)
    const themeBtn = document.getElementById('themeToggleBtn');
    if (themeBtn) {
        themeBtn.addEventListener('click', () => ThemeManager.toggleTheme());
    }

    // Initialize core features
    initStickyNav();
    FloatingCartWidget.init(); // Initialize floating cart widget for mobile
    enhanceAddToCart();
    initSmoothScroll();
    initAlertAutoDismiss();
    initBackToTop();
    fixMobileDropdownScroll(); // Fix scrolling on mobile when dropdown is open

    // Initialize form enhancements
    enhanceFormValidation();
    enhanceQuantityInputs();
    initDeleteConfirmation();

    // Initialize UI enhancements
    enhanceSearch();

    // Initialize lazy loading if supported
    if ('IntersectionObserver' in window) {
        initLazyLoading();
    }

    // Fix mobile dropdown scroll
    fixMobileDropdownScroll();

    // Show welcome toast on home page (optional)
    if (window.location.pathname === '/' || window.location.pathname === '/Index') {
        // Uncomment to show welcome message
        // setTimeout(() => {
        //     ToastManager.info('Welcome to Cap & Collar Supply Co.!', '', 3000);
        // }, 500);
    }
});

// Make managers globally available for use in Razor pages
window.ToastManager = ToastManager;
window.SkeletonLoader = SkeletonLoader;
window.ThemeManager = ThemeManager;
window.FloatingCartWidget = FloatingCartWidget;
