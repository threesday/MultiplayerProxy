class_name NetworkProxySystem
extends Node

enum Role{
	proxy_server,
	proxy_client,
	local_client,
	local_server
}

export(PackedScene) var _host_tcp_client_scene
export(PackedScene) var _client_tcp_client_scene

var _port: int = 5001
var _peer: NetworkedMultiplayerENet = NetworkedMultiplayerENet.new()
var _role

func _ready():
	pass

func dispose():
	for child in get_children():
		child.queue_free()
	_role = null
	get_tree().network_peer = null
	_peer = NetworkedMultiplayerENet.new()

func start_local_server(local_server_port: int, refuse_connections: bool = true):
	if _role != null:
		print("Cannot start local server. The network system already has role: " + str(_role))
		return
	_role = Role.local_server
	_peer.set_bind_ip("127.0.0.1")
	var candidate_port = 1000
	var create_server_result = ERR_ALREADY_IN_USE
	if local_server_port == 0:
		while create_server_result != OK and local_server_port < 65535:
			create_server_result = _peer.create_server(candidate_port)
			candidate_port += 1
	else:
		create_server_result = _peer.create_server(local_server_port)
	if create_server_result != OK:
		print_debug("Server could not be started. Port is not available.")
		dispose()
		return	
	_peer.refuse_new_connections = refuse_connections
	get_tree().network_peer = _peer

func start_proxy_server(proxy_address: String, proxy_port: int, proxy_udp_port: int, room_name: String, local_server_port: int):
	if _role != null:
		print("Cannot start proxy server. The network system already has role: " + str(_role))
		return
	_role = Role.proxy_server
	_peer.set_bind_ip("127.0.0.1")
	_peer.create_server(local_server_port)
	var host_tcp_client = _host_tcp_client_scene.instance()
	host_tcp_client.set_connection_information(proxy_address, proxy_port, proxy_udp_port, room_name, local_server_port)
	host_tcp_client.connect("tcp_proxy_connection_lost", self, "_host_tcp_proxy_connection_lost")
	add_child(host_tcp_client)
	get_tree().network_peer = _peer

func start_local_client(host_address: String, host_port: int):
	if _role != null:
		print("Cannot start local client. The network system already has role: " + str(_role))
		return
	_role = Role.local_client
	print_debug("Starting local client.")
	_peer = NetworkedMultiplayerENet.new()
	_peer.create_client(host_address, host_port)
	get_tree().network_peer = _peer

func start_proxy_client(proxy_address: String, proxy_port: int, proxy_udp_port: int, room_name: String):
	if _role != null:
		print("Cannot start proxy server. The network system already has role: " + str(_role))
		return
	_role = Role.proxy_client
	print_debug("Starting networked proxy client.")
	var client_tcp_client = _client_tcp_client_scene.instance();
	client_tcp_client.connect("tcp_proxy_connection_lost", self, "_client_tcp_proxy_connection_lost")
	client_tcp_client.connect("room_name_does_not_exist", self, "_room_name_does_not_exist")
	client_tcp_client.set_connection_information(proxy_address, proxy_port, proxy_udp_port, room_name)
	client_tcp_client.connect("client_proxy_negotiated", self, "_client_proxy_negotiated")
	add_child(client_tcp_client)

func _client_proxy_negotiated(host_address: String, proxy_udp_port: int, client_port: int, udp_client: PacketPeerUDP):
	print("Proxy negotiated. Creating client.")
	udp_client.close()
	udp_client = null
	_peer.create_client(host_address, proxy_udp_port, 0, 0, client_port)
	_peer.connect("server_disconnected", self, "_client_udp_connection_lost")
	get_tree().network_peer = _peer
	_peer.connect("connection_failed", self, "_connection_failed")
	_peer.connect("connection_succeeded", self, "_connection_succeded")

func _connection_failed():
	print("Connection failed.")

func _connection_succeded():
	print("Connection succeeded.")

func _host_tcp_proxy_connection_lost():
	dispose()

func _client_tcp_proxy_connection_lost():
	dispose()

func _client_udp_connection_lost():
	print_debug("Client UDP connection was lost.")
	dispose()

func _room_name_does_not_exist():
	dispose()