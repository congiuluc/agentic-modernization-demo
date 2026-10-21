(() => {
  document.documentElement.classList.add("js");
  const themeButton = document.getElementById("theme-toggle");
  const status = document.getElementById("ui-status");

  function syncTheme() {
    const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
    const label = `Switch to ${next} theme`;
    themeButton.setAttribute("aria-label", label);
    themeButton.title = label;
    document.querySelectorAll("a[href]").forEach((link) => {
      const url = new URL(link.getAttribute("href"), location.href);
      if (url.origin === location.origin && url.pathname !== location.pathname) {
        url.searchParams.set("scoutTheme", document.documentElement.dataset.theme);
        link.href = url.href;
      }
    });
  }
  themeButton.addEventListener("click", () => {
    const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
    document.documentElement.dataset.theme = next;
    const url = new URL(location.href);
    url.searchParams.set("scoutTheme", next);
    history.replaceState(null, "", url);
    syncTheme();
  });
  let printTheme;
  window.addEventListener("beforeprint", () => {
    printTheme = document.documentElement.dataset.theme;
    document.documentElement.dataset.theme = "light";
  });
  window.addEventListener("afterprint", () => {
    if (printTheme) document.documentElement.dataset.theme = printTheme;
    printTheme = undefined;
  });
  syncTheme();

  const headings = [...document.querySelectorAll(".docs-body h2[id]")];
  const contents = document.getElementById("page-contents");
  if (headings.length) {
    contents.replaceChildren();
    headings.forEach((heading) => {
      const link = document.createElement("a");
      link.href = `#${heading.id}`;
      link.textContent = heading.textContent;
      contents.appendChild(link);
    });
    function markCurrent(id) {
      contents.querySelectorAll("a").forEach((link) => {
        if (link.hash === `#${id}`) link.setAttribute("aria-current", "location");
        else link.removeAttribute("aria-current");
      });
    }
    const initial = headings.find(heading => `#${heading.id}` === location.hash) || headings[0];
    markCurrent(initial.id);
    const observer = new IntersectionObserver((entries) => {
      const visible = entries.filter(entry => entry.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
      if (visible.length) markCurrent(visible[0].target.id);
    }, { rootMargin: "-15% 0px -65% 0px" });
    headings.forEach(heading => observer.observe(heading));
    contents.addEventListener("click", (event) => {
      const link = event.target.closest("a");
      if (link) markCurrent(link.hash.substring(1));
    });
  }
  const panel = document.getElementById("contents-panel");
  const compactLayout = matchMedia("(max-width: 760px)");
  panel.open = !compactLayout.matches;
  compactLayout.addEventListener("change", event => { panel.open = !event.matches; });

  document.querySelectorAll(".docs-body pre").forEach((pre, index) => {
    const code = pre.querySelector("code");
    if (!code) return;
    const wrapper = document.createElement("div");
    wrapper.className = "code-block";
    pre.before(wrapper);
    wrapper.appendChild(pre);
    const button = document.createElement("button");
    button.type = "button";
    button.className = "copy-button";
    button.textContent = "Copy";
    button.setAttribute("aria-label", `Copy code example ${index + 1}`);
    button.addEventListener("click", async () => {
      button.disabled = true;
      try {
        if (!navigator.clipboard) throw new Error("Clipboard access is unavailable in this browser.");
        await navigator.clipboard.writeText(code.textContent);
        button.textContent = "Copied";
        status.textContent = `Code example ${index + 1} copied to the clipboard.`;
      } catch (error) {
        button.textContent = "Copy manually";
        status.textContent = `${error.message} Select the code and use your system copy shortcut.`;
      } finally {
        button.disabled = false;
        setTimeout(() => { button.textContent = "Copy"; }, 2500);
      }
    });
    wrapper.appendChild(button);
  });
})();
