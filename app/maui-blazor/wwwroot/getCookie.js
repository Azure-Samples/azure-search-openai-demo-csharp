function getCookie(cname) {
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var arr = ca[i].split('=');
        if (arr[0] == cname)
            return arr[1]
    }
    return "";
}