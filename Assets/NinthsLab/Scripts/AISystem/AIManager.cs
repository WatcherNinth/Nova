/// 它订阅AIEventDispatcher的OnPlayerInputString事件
/// 然后在这个脚本里完成处理逻辑，包括调用各个Model生成标准request，再调用AIClient发送请求
/// 最后用OnAIResponseReceived把结果分发出去