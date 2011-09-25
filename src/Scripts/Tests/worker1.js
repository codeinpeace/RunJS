onmessage = function (event) {
    var num = event.data.toString().toInt();
    if (isNaN(num)) return postMessage('NaN');
    postMessage("" + num * 2);
};