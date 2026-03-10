const links = document.querySelectorAll("[data-target]");
const panels = document.querySelectorAll(".panel");
const navLinks = document.querySelectorAll(".nav-link");
const menuBtn = document.getElementById("menuBtn");
const mainNav = document.getElementById("mainNav");

function activateSection(target) {
  panels.forEach((panel) => {
    panel.classList.toggle("active", panel.id === target);
  });

  navLinks.forEach((link) => {
    link.classList.toggle("active", link.dataset.target === target);
  });
}

links.forEach((link) => {
  link.addEventListener("click", (event) => {
    const target = link.dataset.target;
    if (!target) return;

    event.preventDefault();
    activateSection(target);
    history.replaceState(null, "", `#${target}`);

    if (mainNav.classList.contains("open")) {
      mainNav.classList.remove("open");
    }
  });
});

window.addEventListener("load", () => {
  const hash = window.location.hash.replace("#", "");
  const allowed = ["inicio", "download", "contrate"];

  if (allowed.includes(hash)) {
    activateSection(hash);
  } else {
    activateSection("inicio");
  }
});

menuBtn.addEventListener("click", () => {
  mainNav.classList.toggle("open");
});
