/* NLWC EMS theme toggle: sun/moon icon button. Honors saved choice, else the OS setting. */
(function () {
  var KEY = 'nlwc-theme', root = document.documentElement;
  var SUN = '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>';
  var MOON = '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"/></svg>';
  function saved() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function current() { return root.getAttribute('data-theme') || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'); }
  function apply(t, persist) {
    root.setAttribute('data-theme', t);
    if (persist) { try { localStorage.setItem(KEY, t); } catch (e) {} }
    document.querySelectorAll('.theme-toggle').forEach(function (b) {
      var dark = t === 'dark';
      b.innerHTML = dark ? SUN : MOON;            /* icon shows what you will switch to */
      b.setAttribute('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode');
      b.setAttribute('title', dark ? 'Light mode' : 'Dark mode');
      b.setAttribute('aria-pressed', String(dark));
    });
  }
  if (saved()) root.setAttribute('data-theme', saved());
  document.addEventListener('DOMContentLoaded', function () {
    var slots = document.querySelectorAll('[data-theme-slot]');
    if (!slots.length) { var s = document.createElement('span'); s.style.cssText = 'position:fixed;right:12px;bottom:12px;z-index:200'; s.setAttribute('data-theme-slot', ''); document.body.appendChild(s); slots = [s]; }
    slots.forEach(function (slot) {
      var b = document.createElement('button'); b.type = 'button'; b.className = 'theme-toggle';
      b.addEventListener('click', function () { apply(current() === 'dark' ? 'light' : 'dark', true); });
      slot.appendChild(b);
    });
    apply(current(), false);
  });
})();
