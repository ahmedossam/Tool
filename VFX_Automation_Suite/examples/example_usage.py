"""
VFX Automation Suite - Example Usage Scripts
Demonstrates how to use the tools programmatically
"""

from core.logger import get_logger
from tools.quality_checker import QualityChecker
from tools.asset_organizer import AssetOrganizer
from tools.format_converter import FormatConverter
from tools.batch_processor import BatchProcessor
import sys
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))


logger = get_logger()


def example_batch_processing():
    """Example: Batch process images"""
    print("\n" + "="*60)
    print("EXAMPLE 1: Batch Image Processing")
    print("="*60)

    processor = BatchProcessor()

    # Example: Resize and convert multiple images
    files = [
        Path("./test_images/image1.png"),
        Path("./test_images/image2.png"),
        Path("./test_images/image3.png")
    ]

    operations = [
        {
            "type": "resize",
            "width": 1920,
            "height": 1080,
            "maintain_aspect": True
        },
        {
            "type": "watermark",
            "text": "PREVIEW",
            "position": "bottom-right",
            "opacity": 128
        },
        {
            "type": "convert",
            "format": "jpg",
            "quality": 90
        }
    ]

    result = processor.execute(
        files=files,
        operations=operations,
        output_dir="./output/batch_processed",
        parallel=True,
        max_workers=4
    )

    print(f"\n✓ Processed: {result['processed']} files")
    print(f"✗ Failed: {result['failed']} files")


def example_format_conversion():
    """Example: Convert formats"""
    print("\n" + "="*60)
    print("EXAMPLE 2: Format Conversion")
    print("="*60)

    converter = FormatConverter()

    # Example 1: Image sequence to video
    print("\n--- Converting image sequence to video ---")
    result = converter.execute(
        input_path="./renders/sequence",
        output_path="./output/final_render.mp4",
        conversion_type="image_sequence_to_video",
        options={
            "framerate": 24,
            "codec": "libx264",
            "quality": 18,  # Lower = better quality
            "pattern": "frame_%04d.png",
            "start_number": 0
        }
    )
    print(f"✓ Created video: {result['output_file']}")

    # Example 2: Video to image sequence
    print("\n--- Converting video to image sequence ---")
    result = converter.execute(
        input_path="./input/video.mp4",
        output_path="./output/frames",
        conversion_type="video_to_image_sequence",
        options={
            "format": "png",
            "pattern": "frame_%04d",
            "start_number": 1
        }
    )
    print(f"✓ Extracted {result['frame_count']} frames")

    # Example 3: Create contact sheet
    print("\n--- Creating contact sheet ---")
    result = converter.execute(
        input_path="./renders/sequence",
        output_path="./output/contact_sheet.jpg",
        conversion_type="contact_sheet",
        options={
            "columns": 5,
            "thumb_size": 200,
            "spacing": 10
        }
    )
    print(f"✓ Contact sheet created: {result['output_file']}")

    # Example 4: Create sprite sheet
    print("\n--- Creating sprite sheet ---")
    result = converter.execute(
        input_path="./sprites",
        output_path="./output/sprite_sheet.png",
        conversion_type="sprite_sheet",
        options={
            "columns": 8,
            "spacing": 0
        }
    )
    print(f"✓ Sprite sheet created with {result['sprite_count']} sprites")


def example_asset_organization():
    """Example: Organize assets"""
    print("\n" + "="*60)
    print("EXAMPLE 3: Asset Organization")
    print("="*60)

    organizer = AssetOrganizer()

    # Example 1: Organize by type
    print("\n--- Organizing by file type ---")
    result = organizer.execute(
        source_dir="./messy_assets",
        target_dir="./organized/by_type",
        mode="by_type",
        options={"copy": True}  # Copy instead of move
    )
    print(f"✓ Organized {result['statistics']['total']} files")

    # Example 2: Find duplicates
    print("\n--- Finding duplicate files ---")
    result = organizer.execute(
        source_dir="./assets",
        target_dir="./organized/duplicates",
        mode="find_duplicates",
        options={
            "delete_duplicates": False,
            "move_to_folder": True
        }
    )
    print(f"✓ Found {result['duplicate_files']} duplicate files")
    print(f"  Space that could be saved: {result['space_saved_mb']:.2f} MB")

    # Example 3: Create asset database
    print("\n--- Creating asset database ---")
    result = organizer.execute(
        source_dir="./asset_library",
        target_dir="./database",
        mode="create_database",
        options={
            "include_thumbnails": True,
            "thumbnail_size": 200
        }
    )
    print(f"✓ Database created with {result['asset_count']} assets")
    print(f"  Database file: {result['database_file']}")

    # Example 4: Rename sequence
    print("\n--- Renaming image sequence ---")
    result = organizer.execute(
        source_dir="./raw_sequence",
        target_dir="./renamed_sequence",
        mode="rename_sequence",
        options={
            "pattern": "shot_001_{counter:04d}",
            "start_number": 1001,
            "extension": "png",
            "copy": True
        }
    )
    print(f"✓ Renamed {result['files_renamed']} files")

    # Example 5: Cleanup
    print("\n--- Cleaning up temp files ---")
    result = organizer.execute(
        source_dir="./project",
        target_dir="./project",  # Same directory
        mode="cleanup",
        options={
            "remove_empty_folders": True,
            "remove_temp_files": True,
            "temp_patterns": ["*.tmp", "*.temp", "*~", ".DS_Store", "Thumbs.db"]
        }
    )
    print(f"✓ Removed {result['temp_files_removed']} temp files")
    print(f"✓ Removed {result['empty_folders_removed']} empty folders")
    print(f"  Space freed: {result['space_freed_mb']:.2f} MB")


def example_quality_control():
    """Example: Quality control checks"""
    print("\n" + "="*60)
    print("EXAMPLE 4: Quality Control")
    print("="*60)

    checker = QualityChecker()

    # Run all quality checks
    result = checker.execute(
        source_dir="./final_delivery",
        # Or specify: ["missing_frames", "resolution", "naming_convention"]
        checks=["all"],
        output_report="./qc_reports/delivery_qc.json"
    )

    print(f"\n{'='*60}")
    print("QUALITY CONTROL RESULTS")
    print(f"{'='*60}")
    print(f"Status: {'✓ PASSED' if result['passed'] else '✗ FAILED'}")
    print(f"Issues: {len(result['issues'])}")
    print(f"Warnings: {len(result['warnings'])}")

    if result['issues']:
        print(f"\n⚠️  ISSUES FOUND:")
        for issue in result['issues'][:5]:  # Show first 5
            print(f"  • [{issue['check']}] {issue['message']}")

        if len(result['issues']) > 5:
            print(f"  ... and {len(result['issues']) - 5} more issues")

    if result['warnings']:
        print(f"\n⚠️  WARNINGS:")
        for warning in result['warnings'][:5]:  # Show first 5
            print(f"  • [{warning['check']}] {warning['message']}")

        if len(result['warnings']) > 5:
            print(f"  ... and {len(result['warnings']) - 5} more warnings")

    print(f"\n📄 Full report saved to: ./qc_reports/delivery_qc.json")


def example_complete_workflow():
    """Example: Complete VFX workflow"""
    print("\n" + "="*60)
    print("EXAMPLE 5: Complete Workflow")
    print("="*60)
    print("Scenario: Prepare renders for client delivery")

    # Step 1: Organize raw renders
    print("\n[1/5] Organizing raw renders...")
    organizer = AssetOrganizer()
    organizer.execute(
        source_dir="./raw_renders",
        target_dir="./organized_renders",
        mode="by_type",
        options={"copy": True}
    )

    # Step 2: Run quality checks
    print("\n[2/5] Running quality checks...")
    checker = QualityChecker()
    qc_result = checker.execute(
        source_dir="./organized_renders/images",
        checks=["missing_frames", "resolution", "corruption"],
        output_report="./qc_report.json"
    )

    if not qc_result['passed']:
        print("⚠️  Quality checks failed! Please review issues before proceeding.")
        return

    # Step 3: Batch process images (resize, watermark)
    print("\n[3/5] Processing images...")
    processor = BatchProcessor()

    # Get all PNG files
    image_files = list(Path("./organized_renders/images").glob("*.png"))

    processor.execute(
        files=image_files,
        operations=[
            {"type": "resize", "width": 1920, "height": 1080},
            {"type": "watermark", "text": "CLIENT REVIEW",
                "position": "bottom-right"},
            {"type": "convert", "format": "jpg", "quality": 90}
        ],
        output_dir="./delivery/images",
        parallel=True
    )

    # Step 4: Create video from sequence
    print("\n[4/5] Creating video...")
    converter = FormatConverter()
    converter.execute(
        input_path="./delivery/images",
        output_path="./delivery/review_video.mp4",
        conversion_type="image_sequence_to_video",
        options={"framerate": 24, "quality": 18}
    )

    # Step 5: Create contact sheet for quick review
    print("\n[5/5] Creating contact sheet...")
    converter.execute(
        input_path="./delivery/images",
        output_path="./delivery/contact_sheet.jpg",
        conversion_type="contact_sheet",
        options={"columns": 6, "thumb_size": 200}
    )

    print("\n" + "="*60)
    print("✓ WORKFLOW COMPLETE!")
    print("="*60)
    print("Deliverables created:")
    print("  • ./delivery/images/ - Processed image sequence")
    print("  • ./delivery/review_video.mp4 - Video for review")
    print("  • ./delivery/contact_sheet.jpg - Quick reference")
    print("  • ./qc_report.json - Quality control report")


def main():
    """Run all examples"""
    print("\n" + "="*80)
    print(" "*20 + "VFX AUTOMATION SUITE - EXAMPLES")
    print("="*80)

    examples = [
        ("Batch Processing", example_batch_processing),
        ("Format Conversion", example_format_conversion),
        ("Asset Organization", example_asset_organization),
        ("Quality Control", example_quality_control),
        ("Complete Workflow", example_complete_workflow)
    ]

    print("\nAvailable examples:")
    for i, (name, _) in enumerate(examples, 1):
        print(f"  {i}. {name}")
    print(f"  {len(examples) + 1}. Run all examples")
    print("  0. Exit")

    try:
        choice = input("\nSelect example to run (0-6): ").strip()
        choice = int(choice)

        if choice == 0:
            print("Goodbye!")
            return
        elif choice == len(examples) + 1:
            for name, func in examples:
                try:
                    func()
                except Exception as e:
                    print(f"\n✗ Error in {name}: {e}")
        elif 1 <= choice <= len(examples):
            name, func = examples[choice - 1]
            try:
                func()
            except Exception as e:
                print(f"\n✗ Error: {e}")
        else:
            print("Invalid choice!")

    except KeyboardInterrupt:
        print("\n\nInterrupted by user")
    except Exception as e:
        print(f"\n✗ Error: {e}")


if __name__ == "__main__":
    main()
