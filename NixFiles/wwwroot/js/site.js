const themeToggle = document.getElementById("themeToggle");

function setTheme(theme) {
    const normalizedTheme = theme === "light" ? "light" : "dark";
    document.documentElement.dataset.theme = normalizedTheme;
    document.documentElement.dataset.bsTheme = normalizedTheme;
    localStorage.setItem("nixfiles-theme", normalizedTheme);

    if (themeToggle) {
        const isDark = normalizedTheme === "dark";
        themeToggle.setAttribute("aria-label", `Switch to ${isDark ? "light" : "dark"} mode`);
        themeToggle.setAttribute("title", `Switch to ${isDark ? "light" : "dark"} mode`);
    }
}

setTheme(document.documentElement.dataset.theme);

themeToggle?.addEventListener("click", function () {
    const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
    setTheme(nextTheme);
});
