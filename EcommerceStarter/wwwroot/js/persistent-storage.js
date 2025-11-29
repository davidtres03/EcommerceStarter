// Persistent cart and wishlist for guests using localStorage
(function() {
    const CART_KEY = 'guest_cart';
    const WISHLIST_KEY = 'guest_wishlist';
    
    // Cart management
    window.guestCart = {
        get: function() {
            try {
                return JSON.parse(localStorage.getItem(CART_KEY) || '[]');
            } catch { return []; }
        },
        
        add: function(productId, quantity = 1) {
            const cart = this.get();
            const existing = cart.find(item => item.productId === productId);
            
            if (existing) {
                existing.quantity += quantity;
            } else {
                cart.push({ productId, quantity, addedAt: new Date().toISOString() });
            }
            
            localStorage.setItem(CART_KEY, JSON.stringify(cart));
            this.updateBadge();
            return cart;
        },
        
        remove: function(productId) {
            let cart = this.get();
            cart = cart.filter(item => item.productId !== productId);
            localStorage.setItem(CART_KEY, JSON.stringify(cart));
            this.updateBadge();
            return cart;
        },
        
        update: function(productId, quantity) {
            const cart = this.get();
            const item = cart.find(item => item.productId === productId);
            
            if (item) {
                item.quantity = quantity;
                localStorage.setItem(CART_KEY, JSON.stringify(cart));
                this.updateBadge();
            }
            
            return cart;
        },
        
        clear: function() {
            localStorage.removeItem(CART_KEY);
            this.updateBadge();
        },
        
        count: function() {
            return this.get().reduce((sum, item) => sum + item.quantity, 0);
        },
        
        updateBadge: function() {
            const badge = document.querySelector('.cart-badge, .badge');
            if (badge) badge.textContent = this.count();
        }
    };
    
    // Wishlist management
    window.guestWishlist = {
        get: function() {
            try {
                return JSON.parse(localStorage.getItem(WISHLIST_KEY) || '[]');
            } catch { return []; }
        },
        
        add: function(productId) {
            const wishlist = this.get();
            if (!wishlist.includes(productId)) {
                wishlist.push(productId);
                localStorage.setItem(WISHLIST_KEY, JSON.stringify(wishlist));
            }
            return wishlist;
        },
        
        remove: function(productId) {
            let wishlist = this.get();
            wishlist = wishlist.filter(id => id !== productId);
            localStorage.setItem(WISHLIST_KEY, JSON.stringify(wishlist));
            return wishlist;
        },
        
        has: function(productId) {
            return this.get().includes(productId);
        },
        
        clear: function() {
            localStorage.removeItem(WISHLIST_KEY);
        }
    };
    
    // Sync with server for logged-in users
    window.syncGuestDataWithServer = function() {
        const cart = window.guestCart.get();
        const wishlist = window.guestWishlist.get();
        
        if (cart.length > 0 || wishlist.length > 0) {
            fetch('/api/sync-guest-data', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ cart, wishlist })
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    window.guestCart.clear();
                    window.guestWishlist.clear();
                }
            });
        }
    };
    
    // Initialize badge on load
    document.addEventListener('DOMContentLoaded', function() {
        window.guestCart.updateBadge();
    });
})();
