class_name HostUdpClient
extends NetworkProxyClient

signal udp_proxy_connection_lost

export(float) var _timeout_seconds = 10

var _proxy_client: PacketPeerUDP
var _enet_client := PacketPeerUDP.new()
var _local_server_port: int
var _last_receive_timer: float

func _ready():
	print_debug("Host UDP client started.")
	var enet_port = 1000
	while _enet_client.listen(enet_port) != OK:
		enet_port += 1
	_enet_client.connect_to_host("127.0.0.1", _local_server_port)
	pass

func _process(delta):
	_last_receive_timer += delta
	if _last_receive_timer > _timeout_seconds:
		print_debug("Udp connection was lost.")
		emit_signal("udp_proxy_connection_lost")
		queue_free()
	if _enet_client.get_available_packet_count() > 0:
		_proxy_client.put_packet(_enet_client.get_packet())
	if _proxy_client.get_available_packet_count() > 0:
		_enet_client.put_packet(_proxy_client.get_packet())
		_last_receive_timer = 0


func set_connection_information(_udp_client: PacketPeerUDP, local_port: int):
	_proxy_client = _udp_client
	_local_server_port = local_port
