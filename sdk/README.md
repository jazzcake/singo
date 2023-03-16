# singo JavaScript SDK

singoのJavaScript SDKです。

## Install

```
npm i --save singo-sdk
```
or
```
yarn add singo-sdk
```

## Usage (example)

```js
const myScreen = document.querySelector('#my-screen')

// コンストラクタで自分の画面のvideo要素を渡します。
// 구성원에서 자신의 화면의 video 요소를 전달합니다.
const client = new SingoClient(myScreen, {
  signalingServerEndpoint: 'ws://localhost:5000'
})

// joinRoomメソッドで特定の名前のroomへ入ります。
// ここではvideoタグを追加して表示しています
// joinRoom 메서드에서 특정 이름의 room으로 들어갑니다.
// 여기서는 video 태그를 추가하여 표시하고 있습니다.
await client.joinRoom('room name')

// onTrackは新たなclientのstreamを受け取ったときに呼ばれます
// onTrack은 새로운 client의 stream을 받았을 때 불려집니다.
client.onTrack = ((clientId, stream) => {
  const elId = `#partner-${clientId}`;
  const pre = document.getElementById(elId);
  pre?.parentNode.removeChild(pre);

  const $video = document.createElement('video') as HTMLVideoElement;
  $video.id = elId;
  $video.setAttribute('autoplay', 'true')
  $video.setAttribute('playsinline', 'true')
  $video.setAttribute('muted', 'true')
  const pa = document.querySelector('#partners');
  pa.appendChild($video);
  $video.srcObject = stream;
});

// onLeaveはclientが退出したときに呼ばれます。
// ここでは退出したclientのvideoタグを削除しています。
// on Leave는 Client가 퇴출되었을 때 불립니다.
// 여기서는 퇴출된 client의 video 태그를 삭제하고 있습니다.
client.onLeave = ((clientId) => {
  const elId = `#partner-${clientId}`;
  const pre = document.getElementById(elId);
  pre?.parentNode.removeChild(pre);
});

// close
client.close()
```
