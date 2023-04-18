import { waitForElement } from "./waitForElement.js";

export async function listenForIFrameLoaded(selector, onLoaded) {
    const iFrame = await waitForElement(selector);
    if (iFrame) {
        iFrame.onload = () => {
            if (onLoaded) {
                onLoaded();
            }
        };
    } else {
        console.warn(
            `Unable to find element with ${selector} selector.`);
    }
}