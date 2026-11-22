import { tr } from '../../core/tr';
/** Crops and resizes a photo to 240×280 JPEG (ID card ratio), as the prototype did, before upload. */
export function resizePhoto(file: File, w = 240, h = 280): Promise<string> {
  return new Promise((resolve, reject) => {
    if (!file.type.startsWith('image/')) { reject(new Error(tr('ছবির ফাইল বাছাই করুন'))); return; }
    const reader = new FileReader();
    reader.onerror = () => reject(new Error(tr('ছবিটি পড়া যায়নি')));
    reader.onload = () => {
      const img = new Image();
      img.onerror = () => reject(new Error(tr('ছবিটি খোলা যায়নি')));
      img.onload = () => {
        const c = document.createElement('canvas');
        c.width = w; c.height = h;
        const s = Math.max(w / img.width, h / img.height);
        c.getContext('2d')!.drawImage(img, (w - img.width * s) / 2, (h - img.height * s) / 2, img.width * s, img.height * s);
        resolve(c.toDataURL('image/jpeg', 0.82));
      };
      img.src = reader.result as string;
    };
    reader.readAsDataURL(file);
  });
}
