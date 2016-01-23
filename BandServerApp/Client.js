var socket;
var element;

document.addEventListener("DOMContentLoaded", function (event) {
    element = document.getElementById('textOut');
    console.log('about to open socket');
    socket = new WebSocket('ws://localhost:54545');
    console.log('attempted to open socket');

    socket.onopen = function () {
        console.log('socket opened');
    };
    socket.onclose = function () {
        console.log('socket closed');
    };
    socket.onerror = function (err) {
        console.log('error - ' + err);
    };
    socket.onmessage = function (event) {
        if (element) {
            var hrEvent = JSON.parse(event.data);
            element.innerText = 'Heart Rate Reading - ' + hrEvent.SensorReading.HeartRate;
        }
        console.log('message - ' + event.data);
    };
});
