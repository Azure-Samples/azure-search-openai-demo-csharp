import { waitForElement } from "./waitForElement.js";

export async function listenForIFrameLoaded(selector, onLoaded) {
    const iFrame = await waitForElement(selector);
    if (iFrame) {
        iFrame.onload = function () {
            if (onLoaded) {
                onLoaded();
            }
        }
    }
}