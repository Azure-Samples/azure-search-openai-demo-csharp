export function waitForElement(selector) {
    return new Promise(resolve => {
        const element = document.querySelector(selector);
        if (element) {
            return resolve(element);
        }

        const observer = new MutationObserver(_ => {
            const elem = document.querySelector(selector);
            if (elem) {
                resolve(elem);
                observer.disconnect();
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    });
}