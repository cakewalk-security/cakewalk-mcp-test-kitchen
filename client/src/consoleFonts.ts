import { loadStylesheet } from "./loadStylesheet";

const CONSOLE_FONTS_ID = "console-fonts";
const CONSOLE_FONTS_HREF =
  "https://fonts.googleapis.com/css2?family=Fredoka:wght@400;500;600;700&family=Inter:wght@400;500;600;700&display=swap";

export function loadConsoleFonts(): void {
  loadStylesheet(CONSOLE_FONTS_HREF, CONSOLE_FONTS_ID);
}
