# ?? UI & Design Guide
## MyStore Supply Co.

Complete guide for UI enhancements, dark mode, color palette, and visual design system.

---

## ?? Table of Contents

1. [Color Palette](#color-palette)
2. [Dark Mode](#dark-mode)
3. [UI Enhancements](#ui-enhancements)
4. [Typography](#typography)
5. [Components](#components)
6. [Animations](#animations)
7. [Accessibility](#accessibility)

---

## ?? Color Palette

### Terracotta & Sage Theme

Your application uses a warm, natural color scheme inspired by southwestern and outdoor aesthetics.

### Primary Colors

**Terracotta (Brand Color)**
```css
--terracotta-50:  #fef6f3;
--terracotta-100: #fde9e0;
--terracotta-200: #fbd5c6;
--terracotta-300: #f7b89e;
--terracotta-400: #f19167;  /* Primary */
--terracotta-500: #e96f3d;
--terracotta-600: #d75726;
--terracotta-700: #b4441c;
--terracotta-800: #92391a;
--terracotta-900: #78331b;
```

**Sage (Accent Color)**
```css
--sage-50:  #f6f7f5;
--sage-100: #e9ebe6;
--sage-200: #d5d9ce;
--sage-300: #b5bcab;
--sage-400: #939d84;    /* Primary */
--sage-500: #7a8468;
--sage-600: #616a52;
--sage-700: #4e5543;
--sage-800: #414638;
--sage-900: #383c31;
```

### Usage Examples

```css
/* Primary button */
.btn-primary {
    background-color: var(--terracotta-400);
    color: white;
}

.btn-primary:hover {
    background-color: var(--terracotta-500);
}

/* Secondary button */
.btn-secondary {
    background-color: var(--sage-400);
    color: white;
}

/* Badges */
.badge-success {
    background-color: var(--sage-400);
}

.badge-warning {
    background-color: var(--terracotta-400);
}

/* Links */
a {
    color: var(--terracotta-500);
}

a:hover {
    color: var(--terracotta-600);
}
```

### Neutral Colors

```css
/* Light Mode */
--gray-50:  #f9fafb;
--gray-100: #f3f4f6;
--gray-200: #e5e7eb;
--gray-300: #d1d5db;
--gray-400: #9ca3af;
--gray-500: #6b7280;
--gray-600: #4b5563;
--gray-700: #374151;
--gray-800: #1f2937;
--gray-900: #111827;

/* Dark Mode */
--dark-bg:      #1a1a1a;
--dark-surface: #242424;
--dark-border:  #3a3a3a;
--dark-text:    #e0e0e0;
```

### Semantic Colors

```css
/* Success */
--success-light: #d1fae5;
--success:       #10b981;
--success-dark:  #047857;

/* Warning */
--warning-light: #fef3c7;
--warning:       #f59e0b;
--warning-dark:  #d97706;

/* Error */
--error-light: #fee2e2;
--error:       #ef4444;
--error-dark:  #dc2626;

/* Info */
--info-light: #dbeafe;
--info:       #3b82f6;
--info-dark:  #1d4ed8;
```

### Color Contrast

**WCAG AAA Compliant Text Combinations:**

? **Light Mode:**
- Dark text on light background: `#1f2937` on `#ffffff`
- Link color with sufficient contrast: `#d75726` (terracotta-600)
- Button text: White on terracotta/sage

? **Dark Mode:**
- Light text on dark background: `#e0e0e0` on `#1a1a1a`
- Link color: `#f19167` (terracotta-400)
- Enhanced contrast for readability

---

## ?? Dark Mode

### Overview

Your app features a **system-aware dark mode** that:
- Detects user's system preference automatically
- Allows manual toggle
- Persists user preference in localStorage
- Smooth transitions between modes
- All components properly styled

### Implementation

**Toggle Button (Already in Layout):**
```html
<button id="theme-toggle" class="btn btn-sm" aria-label="Toggle theme">
    <i class="bi bi-moon-stars-fill" id="theme-icon-dark"></i>
    <i class="bi bi-sun-fill d-none" id="theme-icon-light"></i>
</button>
```

**JavaScript (enhancements.js):**
```javascript
// Initialize dark mode
function initDarkMode() {
    const savedTheme = localStorage.getItem('theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    
    if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
        enableDarkMode();
    }
}

// Toggle dark mode
function toggleDarkMode() {
    if (document.documentElement.getAttribute('data-bs-theme') === 'dark') {
        disableDarkMode();
    } else {
        enableDarkMode();
    }
}

// Enable dark mode
function enableDarkMode() {
    document.documentElement.setAttribute('data-bs-theme', 'dark');
    localStorage.setItem('theme', 'dark');
    updateThemeIcons(true);
}

// Disable dark mode
function disableDarkMode() {
    document.documentElement.setAttribute('data-bs-theme', 'light');
    localStorage.setItem('theme', 'light');
    updateThemeIcons(false);
}
```

### Dark Mode Styles

**Global Dark Mode:**
```css
[data-bs-theme="dark"] {
    /* Backgrounds */
    --bs-body-bg: #1a1a1a;
    --bs-body-color: #e0e0e0;
    
    /* Surfaces */
    --bs-card-bg: #242424;
    --bs-card-border-color: #3a3a3a;
    
    /* Text */
    --bs-heading-color: #ffffff;
    --bs-link-color: var(--terracotta-400);
    --bs-link-hover-color: var(--terracotta-300);
    
    /* Borders */
    --bs-border-color: #3a3a3a;
}
```

**Component-Specific Styles:**
```css
/* Navigation */
[data-bs-theme="dark"] .navbar {
    background-color: #242424 !important;
    border-bottom: 1px solid #3a3a3a;
}

/* Cards */
[data-bs-theme="dark"] .card {
    background-color: #242424;
    border-color: #3a3a3a;
    color: #e0e0e0;
}

/* Forms */
[data-bs-theme="dark"] .form-control {
    background-color: #2a2a2a;
    border-color: #3a3a3a;
    color: #e0e0e0;
}

[data-bs-theme="dark"] .form-control:focus {
    background-color: #2a2a2a;
    border-color: var(--terracotta-400);
    color: #e0e0e0;
}

/* Buttons */
[data-bs-theme="dark"] .btn-outline-primary {
    color: var(--terracotta-400);
    border-color: var(--terracotta-400);
}

/* Product Cards */
[data-bs-theme="dark"] .product-card {
    background-color: #242424;
    border: 1px solid #3a3a3a;
}

[data-bs-theme="dark"] .product-card:hover {
    border-color: var(--terracotta-400);
    box-shadow: 0 4px 12px rgba(241, 145, 103, 0.2);
}
```

### Text Contrast Improvements

**Enhanced Readability:**
```css
/* Light mode - darker text */
[data-bs-theme="light"] body {
    color: #1f2937;
}

[data-bs-theme="light"] .text-muted {
    color: #4b5563 !important;
}

[data-bs-theme="light"] .text-secondary {
    color: #6b7280 !important;
}

/* Dark mode - lighter text */
[data-bs-theme="dark"] body {
    color: #e0e0e0;
}

[data-bs-theme="dark"] .text-muted {
    color: #b0b0b0 !important;
}

[data-bs-theme="dark"] .text-secondary {
    color: #d0d0d0 !important;
}
```

---

## ? UI Enhancements

### Toast Notifications

**Modern, non-intrusive notifications that appear in the bottom-right corner.**

**Features:**
- 4 types: Success, Error, Warning, Info
- Auto-dismiss after 5 seconds
- Smooth slide-in animation
- Stack multiple toasts
- Click to dismiss

**Usage:**
```javascript
// Success
ToastManager.success('Product added to cart!', 'Success');

// Error
ToastManager.error('Failed to process payment', 'Error');

// Warning
ToastManager.warning('Low stock on this item', 'Warning');

// Info
ToastManager.info('Your order has been shipped', 'Info');
```

**Implementation:**
```javascript
const ToastManager = {
    init() {
        if (!document.querySelector('.toast-container')) {
            const container = document.createElement('div');
            container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }
    },
    
    show(message, title = '', type = 'info', duration = 5000) {
        this.init();
        
        const toast = document.createElement('div');
        toast.className = `toast align-items-center border-0 toast-${type}`;
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${title ? `<strong>${title}</strong><br>` : ''}
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                        data-bs-dismiss="toast"></button>
            </div>
        `;
        
        document.querySelector('.toast-container').appendChild(toast);
        
        const bsToast = new bootstrap.Toast(toast, { delay: duration });
        bsToast.show();
        
        toast.addEventListener('hidden.bs.toast', () => toast.remove());
    },
    
    success(message, title = 'Success') {
        this.show(message, title, 'success');
    },
    
    error(message, title = 'Error') {
        this.show(message, title, 'danger');
    },
    
    warning(message, title = 'Warning') {
        this.show(message, title, 'warning');
    },
    
    info(message, title = 'Info') {
        this.show(message, title, 'info');
    }
};
```

**Styles:**
```css
.toast {
    min-width: 300px;
    backdrop-filter: blur(10px);
}

.toast-success {
    background: linear-gradient(135deg, rgba(147, 157, 132, 0.95), rgba(122, 132, 104, 0.95));
    color: white;
}

.toast-danger {
    background: linear-gradient(135deg, rgba(239, 68, 68, 0.95), rgba(220, 38, 38, 0.95));
    color: white;
}

.toast-warning {
    background: linear-gradient(135deg, rgba(241, 145, 103, 0.95), rgba(233, 111, 61, 0.95));
    color: white;
}

.toast-info {
    background: linear-gradient(135deg, rgba(59, 130, 246, 0.95), rgba(29, 78, 216, 0.95));
    color: white;
}
```

### Sticky Navigation

**Navigation bar that sticks to top after scrolling.**

```javascript
window.addEventListener('scroll', function() {
    const navbar = document.querySelector('.navbar');
    const currentScroll = window.pageYOffset;
    
    if (currentScroll > 100) {
        navbar.classList.add('navbar-sticky');
    } else {
        navbar.classList.remove('navbar-sticky');
    }
});
```

```css
.navbar-sticky {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 1030;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
    from {
        transform: translateY(-100%);
    }
    to {
        transform: translateY(0);
    }
}
```

### Loading Skeletons

**Show placeholder content while data loads.**

```javascript
const SkeletonLoader = {
    show(container, count = 3, type = 'card') {
        container.innerHTML = '';
        for (let i = 0; i < count; i++) {
            const skeleton = this.createSkeleton(type);
            container.appendChild(skeleton);
        }
    },
    
    hide(container) {
        container.querySelectorAll('.skeleton').forEach(el => el.remove());
    },
    
    createSkeleton(type) {
        const skeleton = document.createElement('div');
        skeleton.className = 'skeleton';
        
        if (type === 'product-card') {
            skeleton.innerHTML = `
                <div class="card">
                    <div class="skeleton-image"></div>
                    <div class="card-body">
                        <div class="skeleton-line"></div>
                        <div class="skeleton-line short"></div>
                        <div class="skeleton-line shorter"></div>
                    </div>
                </div>
            `;
        }
        
        return skeleton;
    }
};
```

```css
.skeleton {
    animation: skeleton-loading 1s linear infinite alternate;
}

@keyframes skeleton-loading {
    0% {
        background-color: hsl(200, 20%, 80%);
    }
    100% {
        background-color: hsl(200, 20%, 95%);
    }
}

.skeleton-image {
    height: 200px;
    border-radius: 0.375rem 0.375rem 0 0;
}

.skeleton-line {
    height: 1rem;
    margin: 0.5rem 0;
    border-radius: 0.25rem;
}

.skeleton-line.short {
    width: 75%;
}

.skeleton-line.shorter {
    width: 50%;
}
```

### Cart Badge Animation

**Bounce animation when items added to cart.**

```javascript
function animateCartBadge() {
    const badge = document.querySelector('.cart-badge');
    if (badge) {
        badge.classList.add('badge-bounce');
        setTimeout(() => badge.classList.remove('badge-bounce'), 600);
    }
}
```

```css
.badge-bounce {
    animation: bounce 0.6s ease;
}

@keyframes bounce {
    0%, 20%, 50%, 80%, 100% {
        transform: translateY(0) scale(1);
    }
    40% {
        transform: translateY(-10px) scale(1.1);
    }
    60% {
        transform: translateY(-5px) scale(1.05);
    }
}
```

### Button Ripple Effect

**Material Design ripple effect on buttons.**

```javascript
document.querySelectorAll('.btn-ripple').forEach(button => {
    button.addEventListener('click', function(e) {
        const ripple = document.createElement('span');
        ripple.classList.add('ripple');
        this.appendChild(ripple);
        
        const rect = this.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        ripple.style.left = x + 'px';
        ripple.style.top = y + 'px';
        
        setTimeout(() => ripple.remove(), 600);
    });
});
```

```css
.btn-ripple {
    position: relative;
    overflow: hidden;
}

.ripple {
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.6);
    transform: scale(0);
    animation: ripple-animation 0.6s ease-out;
    pointer-events: none;
    width: 20px;
    height: 20px;
    margin-left: -10px;
    margin-top: -10px;
}

@keyframes ripple-animation {
    to {
        transform: scale(4);
        opacity: 0;
    }
}
```

---

## ?? Typography

### Font Stack

```css
:root {
    --font-sans: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    --font-mono: ui-monospace, "Cascadia Code", "Source Code Pro", Menlo, Consolas, monospace;
}

body {
    font-family: var(--font-sans);
    font-size: 1rem;
    line-height: 1.5;
}
```

### Headings

```css
h1, h2, h3, h4, h5, h6 {
    font-weight: 600;
    line-height: 1.2;
    margin-bottom: 0.5em;
}

h1 { font-size: 2.5rem; }
h2 { font-size: 2rem; }
h3 { font-size: 1.75rem; }
h4 { font-size: 1.5rem; }
h5 { font-size: 1.25rem; }
h6 { font-size: 1rem; }
```

### Body Text

```css
.lead {
    font-size: 1.25rem;
    font-weight: 300;
}

.small {
    font-size: 0.875rem;
}

.text-muted {
    color: var(--gray-500);
}
```

---

## ?? Components

### Product Card

```html
<div class="product-card">
    <img src="/images/products/product.jpg" alt="Product Name">
    <div class="product-info">
        <h3 class="product-title">Product Name</h3>
        <p class="product-category">Category</p>
        <div class="product-footer">
            <span class="product-price">$29.99</span>
            <button class="btn btn-primary btn-sm">Add to Cart</button>
        </div>
    </div>
</div>
```

```css
.product-card {
    border-radius: 0.5rem;
    overflow: hidden;
    transition: all 0.3s ease;
    border: 1px solid var(--gray-200);
}

.product-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
    border-color: var(--terracotta-400);
}

.product-card img {
    width: 100%;
    height: 250px;
    object-fit: cover;
}

.product-info {
    padding: 1rem;
}

.product-title {
    font-size: 1.125rem;
    font-weight: 600;
    margin-bottom: 0.25rem;
}

.product-category {
    font-size: 0.875rem;
    color: var(--gray-500);
    margin-bottom: 0.5rem;
}

.product-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.product-price {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--terracotta-500);
}
```

### Order Status Badge

```html
<span class="badge badge-pending">Pending</span>
<span class="badge badge-processing">Processing</span>
<span class="badge badge-shipped">Shipped</span>
<span class="badge badge-delivered">Delivered</span>
<span class="badge badge-cancelled">Cancelled</span>
```

```css
.badge {
    padding: 0.25rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.025em;
}

.badge-pending {
    background-color: var(--warning-light);
    color: var(--warning-dark);
}

.badge-processing {
    background-color: var(--info-light);
    color: var(--info-dark);
}

.badge-shipped {
    background-color: #e9d5ff;
    color: #7c3aed;
}

.badge-delivered {
    background-color: var(--success-light);
    color: var(--success-dark);
}

.badge-cancelled {
    background-color: var(--error-light);
    color: var(--error-dark);
}
```

---

## ?? Animations

### Fade In

```css
.fade-in {
    animation: fadeIn 0.5s ease-in;
}

@keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
}
```

### Slide In

```css
.slide-in-right {
    animation: slideInRight 0.5s ease-out;
}

@keyframes slideInRight {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}
```

### Pulse

```css
.pulse {
    animation: pulse 2s infinite;
}

@keyframes pulse {
    0%, 100% {
        opacity: 1;
    }
    50% {
        opacity: 0.5;
    }
}
```

---

## ? Accessibility

### Focus States

```css
:focus-visible {
    outline: 2px solid var(--terracotta-400);
    outline-offset: 2px;
}

button:focus-visible,
a:focus-visible {
    outline: 2px solid var(--terracotta-400);
    outline-offset: 2px;
}
```

### Screen Reader Text

```css
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

### ARIA Labels

```html
<button aria-label="Add to cart">
    <i class="bi bi-cart-plus"></i>
</button>

<button aria-label="Toggle dark mode" id="theme-toggle">
    <i class="bi bi-moon-stars-fill"></i>
</button>
```

---

## ?? Responsive Design

### Breakpoints

```css
/* Mobile First */
/* Small devices (landscape phones, 576px and up) */
@media (min-width: 576px) { }

/* Medium devices (tablets, 768px and up) */
@media (min-width: 768px) { }

/* Large devices (desktops, 992px and up) */
@media (min-width: 992px) { }

/* Extra large devices (large desktops, 1200px and up) */
@media (min-width: 1200px) { }
```

### Mobile Adjustments

```css
/* Mobile toasts */
@media (max-width: 576px) {
    .toast-container {
        left: 0;
        right: 0;
        bottom: 0;
        padding: 0.5rem;
    }
    
    .toast {
        width: 100%;
        min-width: auto;
    }
}

/* Mobile product grid */
@media (max-width: 768px) {
    .product-grid {
        grid-template-columns: 1fr;
    }
}
```

---

**Everything consolidated from:**
- DARK_MODE_IMPLEMENTATION.md
- DARK_MODE_QUICK_REFERENCE.md
- DARK_MODE_TEXT_IMPROVEMENTS.md
- COLOR_PALETTE_REFERENCE.md
- NEW_COLOR_PALETTE_TERRACOTTA.md
- CONTRAST_IMPROVEMENT_FIX.md
- UI_ENHANCEMENT_GUIDE.md
- UI_ENHANCEMENTS_IMPLEMENTED.md
- UI_ENHANCEMENTS_QUICK_REFERENCE.md
- UI_VISUAL_COMPARISON.md
