"""제미나이 3x2(또는 cols x rows) 초록 배경 시트 → 게임용 프레임 PNG(LA, 투명) 변환.

사용: python3 Tools/anim_sheet.py <시트.png> <출력폴더> <파일접두어> [--cols 3 --rows 2 --canvas 500 --height 0.62 --foot 0.85]
- 초록 배경 제거(반투명 가장자리의 초록 번짐도 제거)
- 칸마다 그림 덩어리만 남기고, 모든 프레임을 같은 배율로 캔버스에 배치
- 발(가장 아래 줄) 높이를 --foot 위치에, 발의 가로 중심을 캔버스 가운데에 맞춤
- 같은 폴더에 .meta 가 없으면 Archer 프레임의 meta 를 복사(새 guid)
"""
import argparse, os, re, uuid
import numpy as np
from PIL import Image

META_SRC = os.path.join(os.path.dirname(__file__), '..', 'Assets/JSW/Resources/Anim/Archer/archer_shoot_1.png.meta')


def key_green(rgb):
    rgb = rgb.astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    spill = g - np.maximum(r, b)              # 초록 정도
    alpha = 1.0 - np.clip((spill - 25) / 110, 0, 1)
    gray = np.maximum(r, b)                   # 초록 성분을 뺀 밝기 (흑백 그림이라 충분)
    return gray, alpha


def write_meta(png):
    meta = png + '.meta'
    if os.path.exists(meta) or not os.path.exists(META_SRC):
        return
    txt = open(META_SRC, encoding='utf-8').read()
    txt = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, txt, count=1)
    open(meta, 'w', encoding='utf-8').write(txt)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('sheet'); ap.add_argument('out'); ap.add_argument('prefix')
    ap.add_argument('--cols', type=int, default=3); ap.add_argument('--rows', type=int, default=2)
    ap.add_argument('--canvas', type=int, default=500)
    ap.add_argument('--height', type=float, default=0.62, help='가장 큰 프레임의 높이 / 캔버스')
    ap.add_argument('--foot', type=float, default=0.85, help='발 위치 / 캔버스')
    ap.add_argument('--min-blob', type=int, default=40, help='이보다 작은 점(반짝이 잔여물)은 지움')
    ap.add_argument('--faint', type=float, default=0.8, help='평균 불투명도가 이보다 낮은 덩어리는 지움')
    ap.add_argument('--map', default='', help='출력 순서. 예: 1,2,2,3,3,4,4,5,5,6 → 고유 그림을 재사용해 10장 저장')
    ap.add_argument('--inset', type=int, default=0, help='칸 가장자리를 이만큼 잘라냄 (경계선이 있는 시트는 6 정도)')
    a = ap.parse_args()

    src = np.array(Image.open(a.sheet).convert('RGB'))
    gray, alpha = key_green(src)
    H, W = alpha.shape
    cw, ch = W / a.cols, H / a.rows
    from scipy import ndimage
    if a.inset:  # 칸 경계선이 그려진 시트: 경계선 주변을 지움
        for k in range(a.cols + 1):
            x = int(k * cw); alpha[:, max(0, x - a.inset):x + a.inset] = 0
        for k in range(a.rows + 1):
            y = int(k * ch); alpha[max(0, y - a.inset):y + a.inset, :] = 0
    # 시트 전체에서 덩어리를 찾고, 덩어리의 중심이 속한 칸에 배정한다.
    # → 무기 끝이 옆 칸으로 넘어가도 잘리지 않고, 옆 칸에 조각이 남지도 않음
    mask = alpha > 0.15
    lab, n = ndimage.label(mask)
    ids = np.arange(1, n + 1)
    sizes = ndimage.sum(mask, lab, ids)
    means = ndimage.mean(alpha, lab, ids)
    cents = ndimage.center_of_mass(mask, lab, ids)
    owner = np.zeros(n + 1, dtype=int) - 1
    for k, s, m, (cy, cx) in zip(ids, sizes, means, cents):
        # 너무 작은 점, 또는 전체가 반투명한 덩어리(초록빛 반짝이 등)는 지움
        if s < a.min_blob or m < a.faint: continue
        owner[k] = min(int(cy // ch), a.rows - 1) * a.cols + min(int(cx // cw), a.cols - 1)
    cells = []
    for i in range(a.cols * a.rows):
        keep = owner[lab] == i
        al = np.where(keep, alpha, 0); gr = gray
        ys, xs = np.where(al > 0.15)
        t, b, l, rr = ys.min(), ys.max(), xs.min(), xs.max()
        foot = al[max(b - 6, 0):b + 1] > 0.15
        fx = np.where(foot.any(0))[0].mean()
        cells.append(dict(al=al, gr=gr, t=t, b=b, l=l, r=rr, fx=fx))

    maxh = max(c['b'] - c['t'] for c in cells)
    s = a.height * a.canvas / maxh
    os.makedirs(a.out, exist_ok=True)
    order = [int(x) for x in a.map.split(',')] if a.map else list(range(1, len(cells) + 1))
    for i, ci in enumerate(order, 1):
        c = cells[ci - 1]
        L = Image.fromarray(np.clip(c['gr'], 0, 255).astype(np.uint8))
        A = Image.fromarray((c['al'] * 255).astype(np.uint8))
        im = Image.merge('LA', (L, A))
        h, w = c['al'].shape
        im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
        canvas = Image.new('LA', (a.canvas, a.canvas), (0, 0))
        ox = round(a.canvas / 2 - c['fx'] * s)
        oy = round(a.foot * a.canvas - c['b'] * s)
        canvas.paste(im, (ox, oy), im)
        # 잘림 확인
        if ox + c['l'] * s < 0 or ox + c['r'] * s > a.canvas or oy + c['t'] * s < 0:
            print(f'경고: {i}번 프레임이 캔버스 밖으로 잘림 → --canvas 를 키우거나 --height 를 줄이세요')
        p = os.path.join(a.out, f'{a.prefix}_{i}.png')
        canvas.save(p); write_meta(p)
        print(p)
    print(f'배율 {s:.2f}')


if __name__ == '__main__':
    main()
