import time
import random
#pynput
from pynput.mouse import Button, Controller

mouse = Controller()
while 1:
    mouse = Controller()
    #点击eve界面
    mouse.position = (1012, 240)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #点击出站
    mouse.position = (1010, 98)
    time.sleep(1)
    mouse.click(Button.left, 1)
    time.sleep(2)
    #人工干预时间
    time.sleep(2)
    
mouse = Controller()
#获取当前位置
print(mouse.position)
#移动到绝对位置
mouse.position = (100, 100)
time.sleep(1)
#移动到相对位置
mouse.move(50, 50)
time.sleep(1)
#鼠标左击
mouse.press(Button.left)
mouse.release(Button.left)
time.sleep(1)
#鼠标右击
mouse.press(Button.right)
mouse.release(Button.right)
time.sleep(1)
mouse.move(-5, -5)
#鼠标点击n次
mouse.click(Button.left, 1)
time.sleep(1)
mouse.scroll(0, 2)