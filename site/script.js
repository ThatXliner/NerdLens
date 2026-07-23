const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

const revealItems = document.querySelectorAll(".reveal");

if (reduceMotion || !("IntersectionObserver" in window)) {
  revealItems.forEach((item) => item.classList.add("is-visible"));
} else {
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.12 },
  );

  revealItems.forEach((item) => observer.observe(item));
}

const tiltTarget = document.querySelector("[data-tilt]");
const heroVisual = document.querySelector(".hero-visual");

if (tiltTarget && heroVisual && !reduceMotion) {
  heroVisual.addEventListener("pointermove", (event) => {
    const bounds = heroVisual.getBoundingClientRect();
    const x = (event.clientX - bounds.left) / bounds.width - 0.5;
    const y = (event.clientY - bounds.top) / bounds.height - 0.5;
    tiltTarget.style.transform = `rotateY(${x * 8 - 7}deg) rotateX(${
      y * -7 + 3
    }deg) translate3d(${x * 8}px, ${y * 8}px, 0)`;
  });

  heroVisual.addEventListener("pointerleave", () => {
    tiltTarget.style.transform = "";
  });
}

const distance = document.querySelector("#distance");
const distanceValue = document.querySelector("#distance-value");

if (distance && distanceValue) {
  const updateDistance = () => {
    const min = Number(distance.min);
    const max = Number(distance.max);
    const value = Number(distance.value);
    const progress = ((value - min) / (max - min)) * 100;

    distanceValue.textContent = String(value);
    distance.style.background = `linear-gradient(to right, #f47a3c 0%, #f47a3c ${progress}%, #343040 ${progress}%, #343040 100%)`;
  };

  distance.addEventListener("input", updateDistance);
  updateDistance();
}

const copyButton = document.querySelector("[data-copy]");
const setupCommands = `git clone https://github.com/ThatXliner/NerdLens.git
iproxy 12345 12345
cd NerdLens/desktop
bun install --frozen-lockfile
bun run tauri dev`;

if (copyButton) {
  copyButton.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(setupCommands);
      copyButton.textContent = "COPIED";
      window.setTimeout(() => {
        copyButton.textContent = "COPY";
      }, 1600);
    } catch {
      copyButton.textContent = "SELECT";
    }
  });
}
