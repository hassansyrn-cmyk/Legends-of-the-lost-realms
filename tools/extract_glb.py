import struct
import json
import os

path = r'C:\Users\X1 YOGA\Downloads\3d checkpoint.glb'
out_dir = r'C:\Users\X1 YOGA\.gemini\antigravity\brain\8161ef4f-085a-40af-8fa6-849260c8d0c7'

with open(path, 'rb') as f:
    magic, version, length = struct.unpack('<4sII', f.read(12))
    print(f'Magic: {magic}, Version: {version}, Length: {length}')
    chunk_len, chunk_type = struct.unpack('<I4s', f.read(8))
    json_bytes = f.read(chunk_len)
    gltf = json.loads(json_bytes.decode('utf-8'))
    
    # Read binary chunk header
    bin_len, bin_type = struct.unpack('<I4s', f.read(8))
    bin_data = f.read(bin_len)

print('Images in GLTF:', len(gltf.get('images', [])))
buffer_views = gltf.get('bufferViews', [])

for i, img in enumerate(gltf.get('images', [])):
    name = img.get('name', f'image_{i}')
    mime = img.get('mimeType', 'image/png')
    ext = '.png' if 'png' in mime else '.jpg'
    bv_idx = img.get('bufferView')
    bv = buffer_views[bv_idx]
    byte_offset = bv.get('byteOffset', 0)
    byte_length = bv.get('byteLength', 0)
    
    img_bytes = bin_data[byte_offset:byte_offset + byte_length]
    save_path = os.path.join(out_dir, f'{name}{ext}')
    with open(save_path, 'wb') as img_out:
        img_out.write(img_bytes)
    print(f'Saved image {i}: {name}{ext}, size={len(img_bytes)} bytes to {save_path}')

print('Materials in GLTF:')
for m in gltf.get('materials', []):
    print(json.dumps(m, indent=2))
