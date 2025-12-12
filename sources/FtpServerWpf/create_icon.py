#!/usr/bin/env python3
"""
Generate icon for FTPS Server application
Creates a modern server icon with cyan accents matching the app design
"""

from PIL import Image, ImageDraw, ImageFont
import os

def create_ftps_icon():
    """Create a professional FTPS server icon"""
    
    # Icon sizes to generate (Windows .ico supports multiple sizes)
    sizes = [256, 128, 64, 48, 32, 16]
    images = []
    
    # Colors matching the application theme
    bg_color = (15, 20, 25)  # Dark background
    accent_color = (0, 217, 255)  # Cyan accent
    secondary_color = (37, 43, 55)  # Medium dark for depth
    
    for size in sizes:
        # Create image with transparency
        img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        
        # Calculate proportions
        margin = size // 8
        server_width = size - (margin * 2)
        server_height = int(server_width * 1.2)
        
        # Center the server
        server_x = margin
        server_y = (size - server_height) // 2
        
        # Draw server body (rounded rectangle)
        server_rect = [
            server_x,
            server_y,
            server_x + server_width,
            server_y + server_height
        ]
        
        # Draw shadow for depth
        shadow_offset = max(2, size // 64)
        shadow_rect = [
            server_rect[0] + shadow_offset,
            server_rect[1] + shadow_offset,
            server_rect[2] + shadow_offset,
            server_rect[3] + shadow_offset
        ]
        draw.rounded_rectangle(shadow_rect, radius=size//16, fill=(0, 0, 0, 60))
        
        # Draw main server body
        draw.rounded_rectangle(server_rect, radius=size//16, fill=secondary_color)
        
        # Draw server segments (horizontal lines)
        segment_count = 3
        segment_height = server_height // segment_count
        line_thickness = max(1, size // 64)
        
        for i in range(1, segment_count):
            y_pos = server_y + (segment_height * i)
            draw.rectangle([
                server_x,
                y_pos - line_thickness // 2,
                server_x + server_width,
                y_pos + line_thickness // 2
            ], fill=bg_color)
        
        # Draw indicator lights on each segment
        light_size = max(2, size // 32)
        light_spacing = light_size * 2
        
        for seg in range(segment_count):
            segment_y = server_y + (segment_height * seg) + segment_height // 2
            
            # Draw 3 lights per segment
            for light_idx in range(3):
                light_x = server_x + server_width - margin - (light_idx * light_spacing) - light_size
                
                # Make one light cyan (active), others darker
                if light_idx == 0:
                    light_color = accent_color
                else:
                    light_color = (0, 100, 120)
                
                draw.ellipse([
                    light_x,
                    segment_y - light_size // 2,
                    light_x + light_size,
                    segment_y + light_size // 2
                ], fill=light_color)
        
        # Draw lock symbol (indicating secure/FTPS)
        lock_size = size // 4
        lock_x = server_x + server_width - lock_size - margin // 2
        lock_y = server_y - lock_size // 2
        
        # Lock body
        lock_body_height = int(lock_size * 0.6)
        lock_body_rect = [
            lock_x,
            lock_y + lock_size - lock_body_height,
            lock_x + lock_size,
            lock_y + lock_size
        ]
        draw.rounded_rectangle(lock_body_rect, radius=lock_size//8, fill=accent_color)
        
        # Lock shackle (arc)
        shackle_thickness = max(2, size // 48)
        shackle_width = int(lock_size * 0.7)
        shackle_height = int(lock_size * 0.5)
        
        # Draw shackle as a thick arc
        shackle_bbox = [
            lock_x + (lock_size - shackle_width) // 2,
            lock_y + lock_size - lock_body_height - shackle_height,
            lock_x + (lock_size + shackle_width) // 2,
            lock_y + lock_size - lock_body_height
        ]
        
        # Draw outer arc
        draw.arc(shackle_bbox, start=180, end=0, fill=accent_color, width=shackle_thickness)
        
        # Draw keyhole on lock body
        keyhole_size = max(2, lock_size // 6)
        keyhole_x = lock_x + lock_size // 2 - keyhole_size // 2
        keyhole_y = lock_y + lock_size - lock_body_height // 2 - keyhole_size // 2
        
        draw.ellipse([
            keyhole_x,
            keyhole_y,
            keyhole_x + keyhole_size,
            keyhole_y + keyhole_size
        ], fill=bg_color)
        
        # Add to images list
        images.append(img)
    
    return images

def save_icon(images, output_path):
    """Save the images as a multi-resolution .ico file"""
    # Save as .ico with multiple resolutions
    images[0].save(
        output_path,
        format='ICO',
        sizes=[(img.width, img.height) for img in images],
        append_images=images[1:]
    )
    print(f"Icon saved to: {output_path}")
    
    # Also save the largest as PNG for reference
    png_path = output_path.replace('.ico', '_preview.png')
    images[0].save(png_path, format='PNG')
    print(f"Preview saved to: {png_path}")

def main():
    print("Generating FTPS Server application icon...")
    
    # Create icon images
    images = create_ftps_icon()
    
    # Save to outputs directory
    output_path = '/mnt/user-data/outputs/FtpsServerApp/icon.ico'
    save_icon(images, output_path)
    
    print("Icon generation complete!")
    print(f"Generated {len(images)} sizes: 256x256, 128x128, 64x64, 48x48, 32x32, 16x16")

if __name__ == '__main__':
    main()
