class_name ClientTcpClient
extends NetworkProxyClient

enum ConnectionState{
	negotiating_room_name,
	port_requested
}

signal client_proxy_negotiated(client_port)
signal tcp_proxy_connection_lost
signal room_name_does_not_exist

export(float) var _timeout_seconds = 10
export(NodePath) var _timeout_timer_path

onready var _timeout_timer: Timer = get_node(_timeout_timer_path)

var _proxy_port: int = 4001
var _room_name: String
var _proxy_address: String
var _tcp_client := StreamPeerTCP.new()
var _connection_state
var _connected: bool = false
var _udp_client: PacketPeerUDP
var _client_port: int
var _proxy_udp_port: int

func _ready():
	print_debug("Started ClientTcpClient")
	print_debug("Connecting to " + _proxy_address + ":" + str(_proxy_port))
	_timeout_timer.wait_time = _timeout_seconds
	_timeout_timer.start()
	_timeout_timer.connect("timeout", self, "_timeout_timer_timed_out")
	_tcp_client.connect_to_host(_proxy_address, _proxy_port)
	_connection_state = ConnectionState.negotiating_room_name
	pass

func _process_data(data):
	if _connection_state == ConnectionState.negotiating_room_name:
		if data[1].get_string_from_ascii() == "ok":
			print_debug("Room name exists. Setting up connection.")
			var client_port = 1000
			_udp_client = PacketPeerUDP.new()
			while _udp_client.listen(client_port) != OK:
				client_port += 1
			_tcp_client.put_data(str(client_port).to_ascii())
			_client_port = client_port
			_connection_state = ConnectionState.port_requested
		else:
			print_debug("Room name does not exist.")
			emit_signal("room_name_does_not_exist")
			queue_free()
	elif _connection_state == ConnectionState.port_requested:
		emit_signal("client_proxy_negotiated", _proxy_address, _proxy_udp_port, _client_port, _udp_client)
		print_debug("Client proxy negotiated successfully.")
		queue_free()
	pass

func _process(_delta):
	if _connected and (_tcp_client.get_status() == _tcp_client.STATUS_NONE or _tcp_client.get_status() == _tcp_client.STATUS_ERROR):
		print_debug("Client TCP connection lost.")
		emit_signal("tcp_proxy_connection_lost")
		queue_free()
	if !_connected:
		if _tcp_client.get_status() == StreamPeerTCP.STATUS_CONNECTED:
			print("Connected to host. Sending room name: " + _room_name)
			_connected = true
			_tcp_client.put_data(_room_name.to_ascii())
	else:
		if _tcp_client.get_available_bytes() > 0:
			var data = _tcp_client.get_data(_tcp_client.get_available_bytes())
			if data[0] == OK:
				_process_data(data)

func set_connection_information(proxy_address: String, proxy_port: int, proxy_udp_port: int, room_name: String):
	_proxy_port = proxy_port
	_proxy_address = proxy_address
	_room_name = room_name
	_proxy_udp_port = proxy_udp_port

func _timeout_timer_timed_out():
	print_debug("Client TCP connection timed out.")
	queue_free()