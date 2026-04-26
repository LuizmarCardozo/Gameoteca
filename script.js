const links = document.querySelectorAll("[data-target]");
const panels = document.querySelectorAll(".panel");
const navLinks = document.querySelectorAll(".nav-link");
const menuBtn = document.getElementById("menuBtn");
const mainNav = document.getElementById("mainNav");


const bgGifs = document.querySelectorAll(".bg-gif");
let currentGifIndex = 0;


function rotateBackground() {
  if (bgGifs.length === 0) return; // Evita erro se não tiver imagem

  // Remove a classe de todos os gifs
  bgGifs.forEach((gif) => gif.classList.remove("active-bg"));
  
  // Adiciona a classe apenas no gif da vez
  bgGifs[currentGifIndex].classList.add("active-bg");
  
  // Calcula o próximo (volta pro 0 quando chegar no último)
  currentGifIndex = (currentGifIndex + 1) % bgGifs.length;
}

// Lógica de navegação das abas
function activateSection(target) {
  panels.forEach((panel) => {
    panel.classList.toggle("active", panel.id === target);
  });

  navLinks.forEach((link) => {
    link.classList.toggle("active", link.dataset.target === target);
  });
}

// Clique nos links do menu
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


  rotateBackground();
  
  
  setInterval(rotateBackground, 8000); 
});

// Botão do menu mobile
menuBtn.addEventListener("click", () => {
  mainNav.classList.toggle("open");
});
