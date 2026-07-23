const distance = document.querySelector("#distance");
const distanceValue = document.querySelector("#distance-value");

if (distance && distanceValue) {
  const updateDistance = () => {
    const min = Number(distance.min);
    const max = Number(distance.max);
    const value = Number(distance.value);
    const progress = ((value - min) / (max - min)) * 100;

    distanceValue.textContent = String(value);
    distance.style.background = `linear-gradient(to right, #f47a3c 0%, #f47a3c ${progress}%, #34303e ${progress}%, #34303e 100%)`;
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
      copyButton.textContent = "Copied";
      window.setTimeout(() => {
        copyButton.textContent = "Copy";
      }, 1600);
    } catch {
      copyButton.textContent = "Copy failed";
    }
  });
}
