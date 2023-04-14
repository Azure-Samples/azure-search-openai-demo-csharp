export async function wait(milliseconds) {
    return new Promise((resolve) => {
        if (isNaN(milliseconds)) {
            throw new Error("milliseconds not a number");
        }

        setTimeout(() => resolve(), milliseconds);
    });
}