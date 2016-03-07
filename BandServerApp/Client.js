var socket;
var hrElement,
    gyroElement,
    accElement;

document.addEventListener("DOMContentLoaded", function (event) {
    hrElement = document.getElementById('hrOut');
    gyroElement = document.getElementById('gyroOut');
    accElement = document.getElementById('accOut');
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
        var bandEvent = JSON.parse(event.data);
        switch (bandEvent.Type) {
            case 'hr':
                hrElement.innerText = 'Heart Rate Reading - ' + bandEvent.Args.HeartRate;
                break;
            case 'gyro':
                gyroElement.innerText = 'Gyro Reading - (' +
                    bandEvent.Args.AccelerationX +
                    ', ' +
                    bandEvent.Args.AccelerationY +
                    ', ' +
                    bandEvent.Args.AccelerationZ +
                    ') (' +
                    bandEvent.Args.AngularVelocityX +
                    ', ' +
                    bandEvent.Args.AngularVelocityY +
                    ', ' +
                    bandEvent.Args.AngularVelocityZ + ')';
                break;
            case 'acc':
                accElement.innerText = 'Acc Reading - (' +
                     bandEvent.Args.AccelerationX +
                     ', ' +
                     bandEvent.Args.AccelerationY +
                     ', ' +
                     bandEvent.Args.AccelerationZ +
                     ')';
                break;
        }
        console.log('message - ' + event.data);
    };
});
