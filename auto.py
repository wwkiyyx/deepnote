import time
import random
#pynput
from pynput.mouse import Button, Controller

mouse = Controller()
while 1:
    mouse = Controller()
    #点击eve界面
    mouse.position = (1239, 100)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击出站
    mouse.position = (1240, 209)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(20)
    #点击采矿
    mouse.position = (1085, 180)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    n = random.randint(0,9)
    #点击小行星带
    mouse.position = (1122, 225 + n * 20)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击跃迁
    mouse.position = (1085, 81)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(50)
    #点击矿
    mouse.position = (1131, 244 + n * 20)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击环绕
    mouse.position = (1116, 82)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击加速
    mouse.position = (813, 665)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(20)
    #点击锁定
    mouse.position = (1182, 81)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击矿枪1
    mouse.position = (787, 623)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击矿枪2
    mouse.position = (839, 623)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击矿石舱
    mouse.position = (114, 291)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(300)
    #点击空间站
    mouse.position = (682, 390)
    time.sleep(1)
    mouse.click(Button.right, 1)
    time.sleep(2)
    #点击停靠
    mouse.position = (753, 468)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(70)
    #点击矿石仓
    mouse.position = (119, 287)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击矿石
    mouse.position = (291, 303)
    time.sleep(1)
    mouse.press(Button.left)
    time.sleep(1)
    mouse.position = (111, 338)
    time.sleep(1)
    mouse.release(Button.left)
    time.sleep(2)
    #人工干预时间
    time.sleep(10)
