class_name NetworkProxyClient
extends Node

func _pick_available_port() -> int:
	var potential_port = 1000
	var udp_client = PacketPeerUDP.new()
	while udp_client.listen(potential_port) != OK:
		potential_port += 1
	if potential_port <= 65535:
		print_debug("Available port chosen: " + str(potential_port))
	return potential_port
