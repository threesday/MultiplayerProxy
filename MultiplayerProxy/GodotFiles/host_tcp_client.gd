class_name HostTcpClient
extends NetworkProxyClient

enum ConnectionState{
	negotiating_room_name,
	port_requested,
	port_sent
}

signal tcp_proxy_connection_lost

export(PackedScene) var _host_udp_client_scene
export(NodePath) var _timeout_timer_path
export(float) var _timeout_seconds = 10

onready var _timeout_timer: Timer = get_node(_timeout_timer_path)

var _tcp_client := StreamPeerTCP.new()
var _udp_client := PacketPeerUDP.new()
var _connection_state
var _connected: bool = false
var _proxy_host_address: String
var _proxy_host_port: int
var _room_name: String
var _client_port: int
var _local_server_port: int
var _proxy_udp_port: int

func _ready():
	print("Started HostTcpClient")
	_timeout_timer.wait_time = _timeout_seconds
	_timeout_timer.start()
	_timeout_timer.connect("timeout", self, "_timeout_timer_timed_out")
	_tcp_client.connect_to_host(_proxy_host_address, _proxy_host_port)
	_connection_state = ConnectionState.negotiating_room_name
	pass

func _process_data(_data):
	if _connection_state == ConnectionState.negotiating_room_name:
		_connection_state = ConnectionState.port_requested
	elif _connection_state == ConnectionState.port_requested:
		print("Port requested. Picking available client port.")
		_timeout_timer.start()
		_client_port = 1000
		_udp_client = PacketPeerUDP.new()
		while _udp_client.listen(_client_port) != OK:
			_client_port += 1
		_udp_client.connect_to_host(_proxy_host_address, _proxy_udp_port)
		_udp_client.put_packet("connect".to_ascii())
		_tcp_client.put_data(str(_client_port).to_ascii())
		_connection_state = ConnectionState.port_sent
	elif _connection_state == ConnectionState.port_sent:
		print("Port acknowledged. Spawning HostUdpClient")
		var host_udp_client = _host_udp_client_scene.instance()
		host_udp_client.set_connection_information(_udp_client, _local_server_port)
		add_child(host_udp_client)
		_timeout_timer.stop()
		_connection_state = ConnectionState.port_requested
	pass

func _process(_delta):
	var status = _tcp_client.get_status()
	if _connected and (status == _tcp_client.STATUS_NONE or status == _tcp_client.STATUS_ERROR):
		print_debug("TCP Proxy connection lost.")
		emit_signal("tcp_proxy_connection_lost")
		queue_free()
	if !_connected:
		if _tcp_client.get_status() == StreamPeerTCP.STATUS_CONNECTED:
			print("Connection okay, sending room name: " + _room_name)
			_connected = true
			_timeout_timer.stop()
			_tcp_client.put_data(_room_name.to_ascii())
	else:
		if _tcp_client.get_available_bytes() > 0:
			var data = _tcp_client.get_data(_tcp_client.get_available_bytes())
			print(data)
			if data[0] == OK:
				print("Message Received: " + data[1].get_string_from_ascii())
				_process_data(data)

func _timeout_timer_timed_out():
	print_debug("Host TCP connection timed out.")
	queue_free()

func set_connection_information(proxy_host: String, proxy_port: int, proxy_udp_port: int, room_name: String, local_server_port: int):
	_proxy_host_address = proxy_host
	_proxy_host_port = proxy_port
	_room_name = room_name
	_local_server_port = local_server_port
	_proxy_udp_port = proxy_udp_port