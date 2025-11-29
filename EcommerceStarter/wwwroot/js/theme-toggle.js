// Theme toggle with smooth transitions and persistent preference
(function() {
    const THEME_KEY = 'site-theme';
    const DARK = 'dark';
    const LIGHT = 'light';
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const root = document.documentElement;
    const btn = document.getElementById('theme-toggle-btn');

    function setTheme(theme) {
        root.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        document.body.classList.add('theme-transition');
        setTimeout(() => document.body.classList.remove('theme-transition'), 400);
    }

    function getTheme() {
        return localStorage.getItem(THEME_KEY) || (prefersDark ? DARK : LIGHT);
    }

    if (btn) {
        btn.addEventListener('click', function() {
            const current = getTheme();
            setTheme(current === DARK ? LIGHT : DARK);
        });
    }

    // On load, set theme
    setTheme(getTheme());
})();

// Dev grid overlay toggle (dev only)
(function() {
    if (!window.location.hostname.includes('localhost')) return;
    let grid = document.createElement('div');
    grid.id = 'dev-grid-overlay';
    grid.style = 'position:fixed;top:0;left:0;width:100vw;height:100vh;z-index:99999;pointer-events:none;display:none;background: repeating-linear-gradient(to right,rgba(0,0,0,0.07) 0,rgba(0,0,0,0.07) 1px,transparent 1px,transparent 80px), repeating-linear-gradient(to bottom,rgba(0,0,0,0.07) 0,rgba(0,0,0,0.07) 1px,transparent 1px,transparent 80px);';
    document.body.appendChild(grid);
    let btn = document.createElement('button');
    btn.innerText = 'Grid';
    btn.id = 'dev-grid-btn';
    btn.style = 'position:fixed;bottom:24px;left:24px;z-index:100000;background:#222;color:#fff;padding:8px 16px;border-radius:8px;border:none;opacity:0.7;cursor:pointer;';
    btn.onclick = function() {
        grid.style.display = grid.style.display === 'none' ? 'block' : 'none';
    };
    document.body.appendChild(btn);
})();
