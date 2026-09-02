package com.siarheikuchuk.ftpsserver.privacy

import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.font.FontWeight

sealed class PrivacyMdBlock {
    data class Heading(val text: String, val level: Int) : PrivacyMdBlock()
    data class Paragraph(val text: String) : PrivacyMdBlock()
    data class Bullet(val text: String) : PrivacyMdBlock()
}

fun parsePrivacyMarkdown(src: String): List<PrivacyMdBlock> {
    val blocks = mutableListOf<PrivacyMdBlock>()
    val paragraph = StringBuilder()

    fun flushParagraph() {
        val text = paragraph.toString().trim()
        paragraph.setLength(0)
        if (text.isNotEmpty()) blocks += PrivacyMdBlock.Paragraph(text)
    }

    for (raw in src.lineSequence()) {
        val trimmed = raw.trim()
        when {
            trimmed.isEmpty() || trimmed == "[Languages](README.md)" -> flushParagraph()
            trimmed.startsWith("### ") -> {
                flushParagraph()
                blocks += PrivacyMdBlock.Heading(trimmed.removePrefix("### ").trim(), 3)
            }
            trimmed.startsWith("## ") -> {
                flushParagraph()
                blocks += PrivacyMdBlock.Heading(trimmed.removePrefix("## ").trim(), 2)
            }
            trimmed.startsWith("# ") -> {
                flushParagraph()
                blocks += PrivacyMdBlock.Heading(trimmed.removePrefix("# ").trim(), 1)
            }
            trimmed.startsWith("- ") || trimmed.startsWith("* ") -> {
                flushParagraph()
                blocks += PrivacyMdBlock.Bullet(trimmed.substring(2).trim())
            }
            else -> {
                if (paragraph.isNotEmpty()) paragraph.append(' ')
                paragraph.append(trimmed)
            }
        }
    }
    flushParagraph()
    return blocks
}

fun inlineMarkdownToAnnotated(text: String): AnnotatedString {
    val builder = AnnotatedString.Builder()
    val regex = Regex("""\*\*(.+?)\*\*""")
    var index = 0
    for (match in regex.findAll(text)) {
        if (match.range.first > index) {
            builder.append(text.substring(index, match.range.first))
        }
        val start = builder.length
        builder.append(match.groupValues[1])
        builder.addStyle(SpanStyle(fontWeight = FontWeight.Bold), start, builder.length)
        index = match.range.last + 1
    }
    if (index < text.length) builder.append(text.substring(index))
    return builder.toAnnotatedString()
}
