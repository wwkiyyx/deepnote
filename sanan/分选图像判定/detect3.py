import os
import time
import numpy as np
import pymysql
import cv2

if __name__ == '__main__':
    host = 'localhost'
    user = 'root'
    password = '880510'
    db = 'fx'
    sql_select = "SELECT * FROM fileTmpTest2 where file_name like '%\\_3\\_%'"
    sql_delete = "DELETE FROM fileTmpTest2 WHERE file_name = "
    sql_insert = "INSERT INTO resFile2(file_datetime, file_name, lot, dt, d, res_dir1, res_dir3, res1, res2, res3) VALUES "
    res_dir = 'D:\\web\\res'
    while True:
        source = []
        conn = pymysql.connect(host=host, user=user, password=password, database=db)
        cursor = conn.cursor()
        tick = time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()) + sql_select
        if ":11 " in tick:
            print(tick)
        cursor.execute(sql_select)
        results = cursor.fetchall()
        for row in results:
            source.append(row[2])
            print(row[2])
        for image in source:
            try:
                name = image[image.rindex('\\') + 1:len(image)]
                lot = name[0:name.index('_')]
                dt = name[name.index('_') + 1:name.index('_') + 13]
                date = '20' + dt[0:2] + '-' + dt[2:4] + '-' + dt[4:6]
                mat1 = cv2.imread(image.replace('_3_', '_1_'), cv2.IMREAD_COLOR)
                mat3 = cv2.imread(image, cv2.IMREAD_COLOR)
                gray = cv2.inRange(mat3, np.array([120, 120, 120]), np.array([140, 140, 140]))
                pink1 = cv2.inRange(mat1, np.array([120, 0, 120]), np.array([255, 130, 255]))
                pink3 = cv2.inRange(mat3, np.array([120, 0, 120]), np.array([255, 130, 255]))
                white = cv2.inRange(mat1, np.array([230, 230, 230]), np.array([255, 255, 255]))
                mat1 = cv2.subtract(mat1, cv2.merge([white, white, white]))
                mat1 = cv2.subtract(mat1, cv2.merge([pink1, pink1, pink1]))
                mat3 = cv2.subtract(mat3, cv2.merge([gray, gray, gray]))
                mat3 = cv2.subtract(mat3, cv2.merge([pink3, pink3, pink3]))
                mat = cv2.absdiff(mat1, mat3)
                mat_gray = cv2.cvtColor(mat, cv2.COLOR_BGR2GRAY)
                ret_mat, mat_threshold = cv2.threshold(mat_gray, 30, 255, cv2.THRESH_BINARY)
                contours, hierarchy = cv2.findContours(mat_threshold, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
                print(name, ' diff count : ', str(len(contours)))
                if not os.path.exists(res_dir):
                    os.mkdir(res_dir)
                if not os.path.exists(res_dir + '\\' + date):
                    os.mkdir(res_dir + '\\' + date)
                if not os.path.exists(res_dir + '\\' + date + '\\' + lot):
                    os.mkdir(res_dir + '\\' + date + '\\' + lot)
                if not os.path.exists(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Jpg', '')):
                    os.mkdir(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Jpg', ''))
                cv2.imwrite(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Jpg', '') + '\\diff.jpg', mat_threshold)
                cursor.execute(sql_insert + '(\'' + time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()) + '\', \'' \
                               + image.replace('\\', '\\\\') + '\', \'' + lot + '\', \'' + dt + '\', \'' + date + '\', \'' \
                               + res_dir.replace('\\', '\\\\') + '\\\\' + date + '\\\\' + lot + '\\\\' + name.replace('_3_', '_1_').replace('.Jpg', '') \
                               + '\', \'' + res_dir.replace('\\', '\\\\') + '\\\\' + date + '\\\\' + lot + '\\\\' + name.replace('.Jpg', '') \
                               + '\', ' + str(len(contours)) + ', 0, 0)')
                conn.commit()
                cursor.execute(sql_delete + '\'' + image.replace('\\', '\\\\') + '\'')
                conn.commit()
                cursor.execute(sql_delete + '\'' + image.replace('_3_', '_1_').replace('\\', '\\\\') + '\'')
                conn.commit()
            except Exception as ex:
                print(ex)
                exp = open("Exception.txt", mode="a")
                exp.write(time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()))
                exp.write("\n")
                exp.write(image)
                exp.write("\n")
                exp.write(str(ex))
                exp.write("\n")
                exp.write("\n")
                exp.flush()
                exp.close()
        conn.close()
        time.sleep(1)
