// Apply the saved theme before first paint to avoid a flash (kept out of index.html so the CSP needs no inline script)
try { var t = localStorage.getItem('mms.theme'); if (t) document.documentElement.dataset.theme = t; } catch (e) {}
