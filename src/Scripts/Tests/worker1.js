onmessage = function (event) {
    var num = event.data.toString().toInt();
    if (isNaN(num)) return;
    postMessage("" + num * 2);
};