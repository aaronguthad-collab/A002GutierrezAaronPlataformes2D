import os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Levels")


class Level:
    def __init__(self, width, height):
        self.w, self.h = width, height
        self.g = [["." for _ in range(width)] for _ in range(height)]

    def fill(self, x0, x1, y0, y1, ch):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.g[y][x] = ch

    def ground(self, x0, x1, top):
        self.fill(x0, x1, top, self.h - 1, "#")

    def plat(self, x0, x1, top, thick=2):
        self.fill(x0, x1, top, top + thick - 1, "=")

    def put(self, x, y, ch):
        self.g[y][x] = ch

    def coins(self, x0, y, n, step=2):
        for i in range(n):
            self.put(x0 + i * step, y, "C")

    def arc(self, x0, ybase, n=5):
        offs = [0, 1, 2, 1, 0, 0, 0][:n]
        for i in range(n):
            self.put(x0 + i, ybase - offs[i], "C")

    def save(self, name):
        self.validate(name)
        os.makedirs(OUT, exist_ok=True)
        with open(os.path.join(OUT, name + ".txt"), "w", newline="\n") as f:
            f.write("\n".join("".join(r) for r in self.g) + "\n")
        print(name, self.w, "x", self.h)
        print("\n".join("".join(r) for r in self.g))
        counts = {c: sum(r.count(c) for r in self.g) for c in "SBXCPEH"}
        print(counts)

    def validate(self, name):
        for y in range(self.h):
            for x in range(self.w):
                c = self.g[y][x]
                if c in "SPXE":
                    assert y + 1 < self.h and self.g[y + 1][x] in "#=", f"{name}: {c} at {x},{y} not on ground"


def level1():
    L = Level(130, 18)
    top = 13

    L.ground(0, 26, top)
    L.ground(31, 58, top)
    L.ground(63, 84, top)
    L.ground(89, 110, top - 2)
    L.ground(111, 129, top - 4)
    L.fill(84, 84, top, top, "#")

    L.plat(14, 18, top - 4)
    L.plat(28, 31, top - 6)
    L.plat(44, 49, top - 5)
    L.plat(52, 56, top - 8)
    L.plat(70, 74, top - 5)
    L.plat(96, 100, top - 7)

    L.put(2, top - 1, "P")
    L.put(126, top - 5, "E")

    L.put(20, top - 1, "S")
    L.put(40, top - 1, "S")
    L.put(55, top - 1, "S")
    L.put(78, top - 1, "S")
    L.put(104, top - 3, "S")

    L.coins(6, top - 1, 4)
    L.coins(14, top - 5, 3)
    L.arc(26, top - 2)
    L.coins(29, top - 7, 2)
    L.coins(44, top - 6, 3)
    L.coins(52, top - 9, 3)
    L.arc(58, top - 2)
    L.coins(70, top - 6, 3)
    L.arc(84, top - 4)
    L.coins(96, top - 8, 3)
    L.coins(114, top - 5, 5)
    L.put(98, top - 10, "H")
    L.save("Level1")


def level2():
    L = Level(190, 26)
    top = 21
    L.ground(0, 22, top)
    L.ground(27, 40, top)

    L.ground(41, 52, top - 3)
    L.plat(55, 59, top - 7)
    L.plat(62, 66, top - 11)
    L.plat(70, 76, top - 14)
    L.plat(80, 85, top - 10)
    L.ground(53, 92, top + 2)
    L.ground(88, 106, top - 2)

    L.plat(110, 113, top - 3)
    L.plat(117, 120, top - 6)
    L.plat(124, 127, top - 4)
    L.plat(131, 135, top - 7)
    L.ground(139, 160, top - 1)
    L.plat(146, 152, top - 6)
    L.ground(163, 189, top - 4)
    L.plat(168, 172, top - 9)

    L.put(2, top - 1, "P")
    L.put(185, top - 5, "E")

    L.put(15, top - 1, "S")
    L.put(35, top - 1, "S")
    L.put(65, top + 1, "S")
    L.put(98, top - 3, "S")
    L.put(150, top - 2, "S")

    L.put(24, top - 6, "B")
    L.put(60, top - 14, "B")
    L.put(115, top - 10, "B")
    L.put(129, top - 11, "B")
    L.put(176, top - 10, "B")

    L.coins(5, top - 1, 4)
    L.arc(22, top - 2)
    L.coins(30, top - 1, 4)
    L.coins(43, top - 4, 4)
    L.coins(56, top - 8, 3)
    L.coins(63, top - 12, 3)
    L.coins(71, top - 15, 4)
    L.coins(81, top - 11, 3)
    L.coins(58, top + 1, 6)
    L.coins(91, top - 3, 5)
    L.coins(110, top - 4, 2)
    L.coins(117, top - 7, 2)
    L.coins(124, top - 5, 2)
    L.coins(131, top - 8, 3)
    L.coins(147, top - 7, 4)
    L.coins(166, top - 5, 5)
    L.coins(168, top - 10, 3)
    L.put(73, top - 17, "H")
    L.put(156, top - 2, "H")
    L.save("Level2")


def level_final():
    L = Level(96, 22)
    top = 17
    L.ground(0, 18, top)
    L.plat(20, 23, top - 3)
    L.ground(26, 40, top - 1)
    L.plat(32, 36, top - 6)
    L.ground(43, 50, top - 3)

    L.ground(51, 95, top)
    L.fill(51, 52, 0, top, "#")
    L.fill(51, 52, top - 9, top - 4, ".")
    L.fill(93, 95, 0, top, "#")
    L.plat(60, 64, top - 5)
    L.plat(80, 84, top - 5)
    L.plat(70, 74, top - 9)
    L.put(2, top - 1, "P")

    L.put(12, top - 1, "S")
    L.put(22, top - 8, "B")
    L.put(33, top - 2, "S")
    L.put(46, top - 9, "B")

    L.put(84, top - 1, "X")

    L.coins(5, top - 1, 3)
    L.coins(20, top - 4, 2)
    L.coins(32, top - 7, 3)
    L.coins(44, top - 4, 3)
    L.coins(71, top - 10, 3)
    L.put(48, top - 5, "H")
    L.put(62, top - 6, "H")
    L.put(82, top - 6, "H")
    L.save("LevelFinal")


if __name__ == "__main__":
    level1()
    level2()
    level_final()
