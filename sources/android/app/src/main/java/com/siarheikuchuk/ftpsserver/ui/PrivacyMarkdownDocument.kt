package com.siarheikuchuk.ftpsserver.ui

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.siarheikuchuk.ftpsserver.privacy.PrivacyMdBlock
import com.siarheikuchuk.ftpsserver.privacy.inlineMarkdownToAnnotated
import com.siarheikuchuk.ftpsserver.privacy.parsePrivacyMarkdown

@Composable
fun PrivacyMarkdownDocument(markdown: String, modifier: Modifier = Modifier) {
    val blocks = parsePrivacyMarkdown(markdown)
    Column(modifier = modifier) {
        for (block in blocks) {
            when (block) {
                is PrivacyMdBlock.Heading -> {
                    val size = when (block.level) {
                        1 -> 22.sp
                        2 -> 18.sp
                        else -> 16.sp
                    }
                    Text(
                        text = inlineMarkdownToAnnotated(block.text),
                        color = AppColors.text,
                        fontSize = size,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.padding(
                            top = if (block.level == 1) 4.dp else 16.dp,
                            bottom = 8.dp,
                        ),
                    )
                }
                is PrivacyMdBlock.Paragraph -> {
                    Text(
                        text = inlineMarkdownToAnnotated(block.text),
                        color = AppColors.text,
                        fontSize = 14.sp,
                        modifier = Modifier.padding(bottom = 10.dp),
                    )
                }
                is PrivacyMdBlock.Bullet -> {
                    Text(
                        text = inlineMarkdownToAnnotated("•  ${block.text}"),
                        color = AppColors.text,
                        fontSize = 14.sp,
                        modifier = Modifier.padding(start = 8.dp, bottom = 6.dp),
                    )
                }
            }
        }
    }
}
