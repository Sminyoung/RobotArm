from mirobot import Mirobot
from time import sleep
api = Mirobot(portname='COM5', debug=False)

api.home_simultaneous()
sleep(1)

# above block
target_angles = {1:49.0, 2:9.5, 3:-3.0, 4:0.0, 5:0.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)
# attach block
target_angles = {1:49.0, 2:54.5, 3:-3.0, 4:0.0, 5:-51.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)
# suction ON
api.pump_on()
# above block
target_angles = {1:49.0, 2:9.5, 3:-3.0, 4:0.0, 5:0.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)
# back to zero pos
api.go_to_zero()
# over chuck
target_angles = {1:0.0, 2:35.0, 3:0.0, 4:0.0, 5:-40.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)

# suction Blow
#api.pump_blow()
api.pump_off()

# back to zero pos ( fin )
api.go_to_zero()
#api.pump_off()

sleep(2)
#=============== 2동작 ==================

# over chuck( attach block )
target_angles = {1:0.0, 2:36.0, 3:0.0, 4:0.0, 5:-40.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)

# suction ON
api.pump_on()

# back to zero pos
api.go_to_zero()

# above Pot ( inverse J1 )
target_angles = {1:-49.0, 2:9.5, 3:-3.0, 4:0.0, 5:0.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)

# into color Pot
target_angles = {1:-49.0, 2:44.5, 3:-3.0, 4:0.0, 5:-51.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)

# suction Blow
api.pump_off()

# above Pot ( inverse J1 )
target_angles = {1:-49.0, 2:9.5, 3:-3.0, 4:0.0, 5:0.0, 6:0.0}
api.set_joint_angle(target_angles, wait=True)

# back to zero pos
api.go_to_zero()
