class_name NetworkProxy
extends Node

export(PackedScene) var _host_client_scene

var proxy_tcp := StreamPeerTCP.new()
var connected: bool = false
var identified: bool = false
var message_queue: Array = []
var current_message = ""
var connection_result: int = -1
var _started: bool = false

func _ready():
	
	pass

func start():
	connection_result = proxy_tcp.connect_to_host("127.0.0.1", 4000)
	_started = true

func _process_data(data: Array):
	var message = data[1].get_string_from_ascii()
	print("Message Received: " + message)
	var host_client = _host_client_scene.instance()
	host_client.client_address = message
	add_child(host_client)

func _process(_delta):
	if !connected and !_started:
		return
	elif !connected and _started:
		if proxy_tcp.get_status() == StreamPeerTCP.STATUS_CONNECTED:
			print("Connection okay, sending test")
			connected = true
			proxy_tcp.put_data("test".to_ascii())
	if proxy_tcp.get_available_bytes() > 0:
		var data = proxy_tcp.get_data(proxy_tcp.get_available_bytes())
		if data[0] == OK:
			_process_data(data)
