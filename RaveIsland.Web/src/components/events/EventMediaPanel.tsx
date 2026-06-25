import { useRef, useState } from "react";
import { ImagePlus, Loader2, Trash2, Upload } from "lucide-react";
import { FormField } from "../layout/FormField";
import { Button } from "../ui/button";
import { apiDelete, apiUpload, resolveMediaUrl, type EventMediaItem } from "../../lib/api";
import { cn } from "../../lib/utils";

type MediaType = "Cover" | "Banner" | "Gallery";

type EventMediaPanelProps = {
  eventId: string;
  token: string;
  media: EventMediaItem[];
  onMediaChange: (media: EventMediaItem[]) => void;
  onError: (message: string) => void;
  onSuccess: (message: string) => void;
};

function groupMedia(media: EventMediaItem[]) {
  return {
    cover: media.filter((item) => item.mediaType === "Cover"),
    banner: media.filter((item) => item.mediaType === "Banner"),
    gallery: media.filter((item) => item.mediaType === "Gallery"),
  };
}

export function EventMediaPanel({
  eventId,
  token,
  media,
  onMediaChange,
  onError,
  onSuccess,
}: EventMediaPanelProps) {
  const coverInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);
  const galleryInputRef = useRef<HTMLInputElement>(null);
  const [uploadingType, setUploadingType] = useState<MediaType | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const grouped = groupMedia(media);

  async function uploadFiles(files: FileList | File[], mediaType: MediaType) {
    const fileList = Array.from(files);
    if (fileList.length === 0) return;

    setUploadingType(mediaType);
    onError("");

    try {
      let nextMedia = [...media];

      for (const file of fileList) {
        const formData = new FormData();
        formData.append("file", file);
        formData.append("mediaType", mediaType);

        const uploaded = await apiUpload<EventMediaItem>(`/api/events/${eventId}/media`, formData, token);

        if (mediaType === "Cover" || mediaType === "Banner") {
          nextMedia = nextMedia.filter((item) => item.mediaType !== mediaType);
        }

        nextMedia = [...nextMedia, uploaded];
      }

      onMediaChange(
        nextMedia.sort((a, b) => a.displayOrder - b.displayOrder || a.fileName.localeCompare(b.fileName)),
      );
      onSuccess(
        fileList.length === 1 ? `${mediaType} image uploaded.` : `${fileList.length} gallery images uploaded.`,
      );
    } catch (err: unknown) {
      onError(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setUploadingType(null);
    }
  }

  async function removeMedia(item: EventMediaItem) {
    setDeletingId(item.id);
    onError("");

    try {
      await apiDelete(`/api/events/${eventId}/media/${item.id}`, token);
      onMediaChange(media.filter((entry) => entry.id !== item.id));
      onSuccess("Image removed.");
    } catch (err: unknown) {
      onError(err instanceof Error ? err.message : "Failed to remove image");
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="space-y-8">
      <SingleMediaSlot
        label="Cover image"
        hint="Square or portrait, used in listings"
        items={grouped.cover}
        uploading={uploadingType === "Cover"}
        deletingId={deletingId}
        inputRef={coverInputRef}
        onUpload={(files) => void uploadFiles(files, "Cover")}
        onRemove={removeMedia}
      />

      <SingleMediaSlot
        label="Banner"
        hint="Wide hero image for the event page"
        items={grouped.banner}
        uploading={uploadingType === "Banner"}
        deletingId={deletingId}
        inputRef={bannerInputRef}
        onUpload={(files) => void uploadFiles(files, "Banner")}
        onRemove={removeMedia}
      />

      <GallerySection
        items={grouped.gallery}
        uploading={uploadingType === "Gallery"}
        deletingId={deletingId}
        inputRef={galleryInputRef}
        onUpload={(files) => void uploadFiles(files, "Gallery")}
        onRemove={removeMedia}
      />
    </div>
  );
}

type SingleMediaSlotProps = {
  label: string;
  hint: string;
  items: EventMediaItem[];
  uploading: boolean;
  deletingId: string | null;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onUpload: (files: FileList) => void;
  onRemove: (item: EventMediaItem) => void;
};

function SingleMediaSlot({
  label,
  hint,
  items,
  uploading,
  deletingId,
  inputRef,
  onUpload,
  onRemove,
}: SingleMediaSlotProps) {
  const item = items[0];

  return (
    <FormField label={label} description={hint}>
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={(e) => {
          if (e.target.files?.length) {
            onUpload(e.target.files);
            e.target.value = "";
          }
        }}
      />

      {item ? (
        <MediaPreviewCard
          item={item}
          deleting={deletingId === item.id}
          onRemove={() => void onRemove(item)}
          aspectClass="aspect-[4/5] max-w-xs"
        />
      ) : (
        <EmptyUploadPlaceholder />
      )}

      <div className="mt-3 flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={uploading}
          onClick={() => inputRef.current?.click()}
        >
          {uploading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Uploading...
            </>
          ) : (
            <>
              <Upload className="mr-2 h-4 w-4" />
              {item ? "Replace image" : "Upload image"}
            </>
          )}
        </Button>
        {item && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={deletingId === item.id}
            onClick={() => void onRemove(item)}
          >
            {deletingId === item.id ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Trash2 className="mr-2 h-4 w-4" />
            )}
            Remove
          </Button>
        )}
      </div>
    </FormField>
  );
}

type GallerySectionProps = {
  items: EventMediaItem[];
  uploading: boolean;
  deletingId: string | null;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onUpload: (files: FileList) => void;
  onRemove: (item: EventMediaItem) => void;
};

function GallerySection({
  items,
  uploading,
  deletingId,
  inputRef,
  onUpload,
  onRemove,
}: GallerySectionProps) {
  return (
    <FormField
      label="Gallery"
      description="Add multiple photos from past editions or promo shoots."
    >
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        multiple
        className="hidden"
        onChange={(e) => {
          if (e.target.files?.length) {
            onUpload(e.target.files);
            e.target.value = "";
          }
        }}
      />

      {items.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <MediaPreviewCard
              key={item.id}
              item={item}
              deleting={deletingId === item.id}
              onRemove={() => void onRemove(item)}
              aspectClass="aspect-video"
            />
          ))}
        </div>
      ) : (
        <EmptyUploadPlaceholder />
      )}

      <div className="mt-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={uploading}
          onClick={() => inputRef.current?.click()}
        >
          {uploading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Uploading...
            </>
          ) : (
            <>
              <ImagePlus className="mr-2 h-4 w-4" />
              {items.length > 0 ? "Add more photos" : "Add gallery photos"}
            </>
          )}
        </Button>
      </div>
    </FormField>
  );
}

function EmptyUploadPlaceholder() {
  return (
    <div className="flex aspect-video max-w-md items-center justify-center rounded-xl border border-dashed border-border/80 bg-muted/20">
      <div className="text-center text-sm text-muted-foreground">
        <ImagePlus className="mx-auto mb-2 h-8 w-8 opacity-60" />
        No image uploaded yet
      </div>
    </div>
  );
}

type MediaPreviewCardProps = {
  item: EventMediaItem;
  deleting: boolean;
  onRemove: () => void;
  aspectClass: string;
};

function MediaPreviewCard({ item, deleting, onRemove, aspectClass }: MediaPreviewCardProps) {
  const imageUrl = resolveMediaUrl(item.thumbnailUrl ?? item.storageUrl);

  return (
    <div className="group relative overflow-hidden rounded-xl border border-border/80 bg-muted/10">
      <div className={cn("relative w-full overflow-hidden bg-muted/30", aspectClass)}>
        <img
          src={imageUrl}
          alt={item.fileName}
          className="h-full w-full object-cover"
        />
        <button
          type="button"
          aria-label={`Remove ${item.fileName}`}
          disabled={deleting}
          onClick={onRemove}
          className="absolute right-2 top-2 rounded-md bg-background/90 p-2 text-destructive opacity-0 shadow-sm transition-opacity group-hover:opacity-100 disabled:opacity-50"
        >
          {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
        </button>
      </div>
      <p className="truncate px-3 py-2 text-xs text-muted-foreground">{item.fileName}</p>
    </div>
  );
}
